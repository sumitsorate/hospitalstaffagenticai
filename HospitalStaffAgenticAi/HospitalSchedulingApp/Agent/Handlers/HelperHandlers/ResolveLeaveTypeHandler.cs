using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.Tools.HelperTools;
using HospitalSchedulingApp.Common.Enums;
using HospitalSchedulingApp.Dal.Entities;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.HelperHandlers
{
    public class ResolveLeaveTypeToolHandler : IToolHandler
    {
        private readonly ILogger<ResolveLeaveTypeToolHandler> _logger;

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

            string? matchedType = input switch
            {
                "sick" or "illness" or "medical" => "Sick",
                "casual" or "personal" or "urgent" => "Casual",
                "vacation" or "holiday" or "leave" => "Vacation",
                _ => null
            };

            if (matchedType == null)
            {
                _logger.LogInformation("ResolveLeaveType: Invalid input '{Input}'", input);
                return CreateError(call.Id, $"Invalid leave type: '{input}'. Valid types are Sick, Casual, or Vacation.");
            }

            var result = new
            {
                success = true,
                match = new
                {
                    input,
                    matchedType,
                    matchedTypeValue = (int)Enum.Parse(typeof(LeaveType), matchedType)
                }
            };

            _logger.LogInformation("ResolveLeaveType: Mapped '{Input}' to '{MatchedType}'", input, matchedType);
            return new ToolOutput(call.Id, JsonSerializer.Serialize(result));
        }

        private ToolOutput CreateError(string callId, string message)
        {
            var error = new { success = false, error = message };
            return new ToolOutput(callId, JsonSerializer.Serialize(error));
        }
    }
}
