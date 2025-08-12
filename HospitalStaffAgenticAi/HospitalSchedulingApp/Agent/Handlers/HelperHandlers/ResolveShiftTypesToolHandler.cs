using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.MetaResolver;
using HospitalSchedulingApp.Agent.Tools.HelperTools;
using HospitalSchedulingApp.Common.Enums;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.HelperHandlers
{
    public class ResolveShiftTypeToolHandler : IToolHandler
    {
        private readonly IEntityResolver _entityResolver;
        private readonly ILogger<ResolveShiftTypeToolHandler> _logger;

        public ResolveShiftTypeToolHandler(
            IEntityResolver entityResolver,
            ILogger<ResolveShiftTypeToolHandler> logger)
        {
            _entityResolver = entityResolver;
            _logger = logger;
        }

        public string ToolName => ResolveShiftTypeTool.GetTool().Name;

        public async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            string input = root.TryGetProperty("shift", out var shiftProp)
                ? shiftProp.GetString()?.Trim() ?? string.Empty
                : string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                _logger.LogWarning("ResolveShiftType: Shift input is missing.");
                return CreateError(call.Id, "Shift type is required.");
            }

            var resolved = await _entityResolver.ResolveEntitiesAsync(input);

            if (resolved.ShiftType == null)
            {
                _logger.LogInformation("ResolveShiftType: Invalid input '{Input}'", input);
                return CreateError(call.Id, $"Invalid shift type: '{input}'");
            }

            var matchedType = resolved.ShiftType.ShiftTypeName;
            var matchedTypeValue = resolved.ShiftType.ShiftTypeId; // assuming this corresponds to your enum int values

            var result = new
            {
                success = true,
                match = new
                {
                    input,
                    matchedType,
                    matchedTypeValue
                }
            };

            _logger.LogInformation("ResolveShiftType: Mapped '{Input}' to '{MatchedType}'", input, matchedType);
            return new ToolOutput(call.Id, JsonSerializer.Serialize(result));
        }

        private ToolOutput CreateError(string callId, string message)
        {
            var error = new { success = false, error = message };
            return new ToolOutput(callId, JsonSerializer.Serialize(error));
        }
    }
}
