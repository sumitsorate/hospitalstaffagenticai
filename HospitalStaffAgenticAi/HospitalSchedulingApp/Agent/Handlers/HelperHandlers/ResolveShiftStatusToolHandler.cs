using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.Tools.Department;
using HospitalSchedulingApp.Agent.Tools.HelperTools;
using HospitalSchedulingApp.Agent.Tools.LeaveRequest;
using HospitalSchedulingApp.Common.Enums;
using HospitalSchedulingApp.Dal.Entities;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.LeaveRequest
{
    public class ResolveShiftStatusToolHandler : IToolHandler
    {
        private readonly ILogger<ResolveShiftStatusToolHandler> _logger;

        public ResolveShiftStatusToolHandler(ILogger<ResolveShiftStatusToolHandler> logger)
        {
            _logger = logger;
        }

        public string ToolName => ResolveShiftStatusTool.GetTool().Name;

        public async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            string input = root.TryGetProperty("status", out var statusProp)
                ? statusProp.GetString()?.Trim().ToLowerInvariant() ?? string.Empty
                : string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                _logger.LogWarning("ResolveShiftStatus: Status input is missing.");
                return CreateError(call.Id, "Shift status is required.");
            }

            string? matchedStatus = input switch
            {
                "scheduled" or "plan" or "planned" => "Scheduled",
                "assigned" or "assigned to" => "Assigned",
                "completed" or "done" or "finished" => "Completed",
                "cancelled" or "canceled" or "aborted" => "Cancelled",
                "vacant" or "open" or "unassigned" => "Vacant",
                _ => null
            };

            if (matchedStatus == null)
            {
                _logger.LogInformation("ResolveShiftStatus: Invalid input '{Input}'", input);
                return CreateError(call.Id, $"Invalid shift status: '{input}'");
            }

            var result = new
            {
                success = true,
                match = new
                {
                    input,
                    matchedStatus,
                    matchedStatusValue = (int)Enum.Parse(typeof(ShiftStatuses), matchedStatus)
                }
            };

            _logger.LogInformation("ResolveShiftStatus: Mapped '{Input}' to '{MatchedStatus}'", input, matchedStatus);
            return new ToolOutput(call.Id, JsonSerializer.Serialize(result));
        }

        private ToolOutput CreateError(string callId, string message)
        {
            var error = new { success = false, error = message };
            return new ToolOutput(callId, JsonSerializer.Serialize(error));
        }
    }

}
