using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.Tools.HelperTools;
using HospitalSchedulingApp.Common.Enums;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.HelperHandlers
{
    public class ResolveLeaveTypeToolHandler : IToolHandler
    {
        private readonly ILogger<ResolveLeaveTypeToolHandler> _logger;

        private static readonly Dictionary<string, LeaveType> LeaveTypeSynonyms = new(StringComparer.OrdinalIgnoreCase)
        {
            { "sick", LeaveType.Sick },
            { "illness", LeaveType.Sick },
            { "medical", LeaveType.Sick },
            { "casual", LeaveType.Casual },
            { "personal", LeaveType.Casual },
            { "urgent", LeaveType.Casual },
            { "vacation", LeaveType.Vacation },
            { "holiday", LeaveType.Vacation },
            { "annual", LeaveType.Vacation },
            { "earned", LeaveType.Vacation },
            { "leave", LeaveType.Vacation }
        };

        public ResolveLeaveTypeToolHandler(ILogger<ResolveLeaveTypeToolHandler> logger)
        {
            _logger = logger;
        }

        public string ToolName => ResolveLeaveTypeTool.GetTool().Name;

        public async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            string input = root.TryGetProperty("leaveType", out var leaveProp)
                ? leaveProp.GetString()?.Trim().ToLowerInvariant() ?? string.Empty
                : string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                _logger.LogWarning("ResolveLeaveType: Leave input is missing.");
                return CreateError(call.Id, "Leave type is required.");
            }

            if (!LeaveTypeSynonyms.TryGetValue(input, out var leaveType))
            {
                _logger.LogInformation("ResolveLeaveType: Invalid input '{Input}'", input);
                var validTypes = string.Join(", ", Enum.GetNames(typeof(LeaveType)));
                return CreateError(call.Id, $"Invalid leave type: '{input}'. Valid types are: {validTypes}.");
            }

            var result = new
            {
                success = true,
                match = new
                {
                    input,
                    matchedType = leaveType.ToString(),
                    matchedTypeValue = (int)leaveType
                }
            };

            _logger.LogInformation("ResolveLeaveType: Mapped '{Input}' to '{MatchedType}'", input, leaveType);
            return new ToolOutput(call.Id, JsonSerializer.Serialize(result));
        }

        private ToolOutput CreateError(string callId, string message)
        {
            var error = new { success = false, error = message };
            return new ToolOutput(callId, JsonSerializer.Serialize(error));
        }
    }
}
