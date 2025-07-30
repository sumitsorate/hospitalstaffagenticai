using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.Tools.Department;
using HospitalSchedulingApp.Agent.Tools.LeaveRequest;
using HospitalSchedulingApp.Common.Enums;
using HospitalSchedulingApp.Dal.Entities;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.LeaveRequest
{
    public class ResolveLeaveStatusToolHandler : IToolHandler
    {
        private readonly ILogger<ResolveLeaveStatusToolHandler> _logger;

        public ResolveLeaveStatusToolHandler(ILogger<ResolveLeaveStatusToolHandler> logger)
        {
            _logger = logger;
        }
        public string ToolName => ResolveLeaveStatusTool.GetTool().Name;
        public async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            string input = root.TryGetProperty("status", out var statusProp)
                ? statusProp.GetString()?.Trim().ToLowerInvariant() ?? string.Empty
                : string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                _logger.LogWarning("ResolveLeaveStatus: Status input is missing.");
                return CreateError(call.Id, "Leave status is required.");
            }

            string? matchedStatus = input switch
            {
                "pending" or "in progress" or "awaiting" or "waiting" => "Pending",
                "approved" or "approve" or "accept" or "accepted" or "granter" => "Approved",
                "rejected" or "deny" or "denied" or "refused" => "Rejected",
                _ => null
            };

            if (matchedStatus == null)
            {
                _logger.LogInformation("ResolveLeaveStatus: Invalid input '{Input}'", input);
                return CreateError(call.Id, $"Invalid leave status: '{input}'");
            }

            var result = new
            {
                success = true,
                match = new
                {
                    input,
                    matchedStatus,
                    matchedStatusValue = (int)Enum.Parse(typeof(LeaveStatus), matchedStatus)
                }
            };

            _logger.LogInformation("ResolveLeaveStatus: Mapped '{Input}' to '{MatchedStatus}'", input, matchedStatus);
            return new ToolOutput(call.Id, JsonSerializer.Serialize(result));
        }

        private ToolOutput CreateError(string callId, string message)
        {
            var error = new { success = false, error = message };
            return new ToolOutput(callId, JsonSerializer.Serialize(error));
        }
    }

}
