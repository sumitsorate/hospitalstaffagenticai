using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.Tools.HelperTools;
using HospitalSchedulingApp.Common.Enums;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.HelperHandlers
{
    public class ResolveShiftTypeToolHandler : IToolHandler
    {
        private readonly ILogger<ResolveShiftTypeToolHandler> _logger;

        public ResolveShiftTypeToolHandler(ILogger<ResolveShiftTypeToolHandler> logger)
        {
            _logger = logger;
        }

        public string ToolName => ResolveShiftTypeTool.GetTool().Name;

        public async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            string input = root.TryGetProperty("shift", out var shiftProp)
                ? shiftProp.GetString()?.Trim().ToLowerInvariant() ?? string.Empty
                : string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                _logger.LogWarning("ResolveShiftType: Shift input is missing.");
                return CreateError(call.Id, "Shift type is required.");
            }

            string? matchedType = input switch
            {
                "night" or "n" => "Night",
                "morning" or "m" or "day" => "Morning",
                "evening" or "e" => "Evening",
                _ => null
            };

            if (matchedType == null)
            {
                _logger.LogInformation("ResolveShiftType: Invalid input '{Input}'", input);
                return CreateError(call.Id, $"Invalid shift type: '{input}'");
            }

            var result = new
            {
                success = true,
                match = new
                {
                    input,
                    matchedType,
                    matchedTypeValue = (int)Enum.Parse(typeof(ShiftTypes), matchedType)
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
