using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.MetaResolver;
using HospitalSchedulingApp.Agent.Tools.HelperTools;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.LeaveRequest
{
    public class ResolveShiftStatusToolHandler : IToolHandler
    {
        private readonly IEntityResolver _entityResolver;
        private readonly ILogger<ResolveShiftStatusToolHandler> _logger;

        public ResolveShiftStatusToolHandler(
            IEntityResolver entityResolver,
            ILogger<ResolveShiftStatusToolHandler> logger)
        {
            _entityResolver = entityResolver ?? throw new ArgumentNullException(nameof(entityResolver));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string ToolName => ResolveShiftStatusTool.GetTool().Name;

        public async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            if (call == null)
            {
                _logger.LogError("ResolveShiftStatusToolHandler: Tool call was null.");
                return CreateError(Guid.NewGuid().ToString(), "Invalid tool call object.");
            }

            string input = root.TryGetProperty("status", out var statusProp)
                ? statusProp.GetString()?.Trim() ?? string.Empty
                : string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                _logger.LogWarning("ResolveShiftStatus: Status input is missing.");
                return CreateError(call.Id, "Shift status is required.");
            }

            // Use IEntityResolver to resolve shift status entity by phrase
            var resolved = await _entityResolver.ResolveEntitiesAsync(input);

            if (resolved.ShiftStatus == null)
            {
                _logger.LogInformation("ResolveShiftStatus: No matching shift status found for '{Input}'", input);
                return CreateError(call.Id, $"Invalid shift status: '{input}'. Expected a valid shift status.");
            }

            var matchedStatusName = resolved.ShiftStatus.ShiftStatusName;
            var matchedStatusValue = resolved.ShiftStatus.ShiftStatusId; // Assuming ShiftStatusId maps to enum int value

            var result = new
            {
                success = true,
                match = new
                {
                    input,
                    matchedStatus = matchedStatusName,
                    matchedStatusValue
                }
            };

            _logger.LogInformation("ResolveShiftStatus: Mapped '{Input}' to '{MatchedStatus}'", input, matchedStatusName);

            return new ToolOutput(call.Id, JsonSerializer.Serialize(result));
        }

        private ToolOutput CreateError(string callId, string message)
        {
            var error = new { success = false, error = message };
            return new ToolOutput(callId, JsonSerializer.Serialize(error));
        }
    }
}
