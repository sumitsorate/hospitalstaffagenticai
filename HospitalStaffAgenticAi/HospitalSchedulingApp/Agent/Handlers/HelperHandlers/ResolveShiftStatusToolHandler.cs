using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.Tools.HelperTools;
using HospitalSchedulingApp.Agent.Tools.LeaveRequest;
using HospitalSchedulingApp.Common.Enums;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.LeaveRequest
{
    public class ResolveShiftStatusToolHandler : IToolHandler
    {
        private readonly ILogger<ResolveShiftStatusToolHandler> _logger;

        public ResolveShiftStatusToolHandler(ILogger<ResolveShiftStatusToolHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string ToolName => ResolveShiftStatusTool.GetTool().Name;

        public async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            // Defensive null check for call
            if (call == null)
            {
                _logger.LogError("ResolveShiftStatusToolHandler: Tool call was null.");
                return CreateError(Guid.NewGuid().ToString(), "Invalid tool call object.");
            }

            // Extract status from JSON
            string input = root.TryGetProperty("status", out var statusProp)
                ? statusProp.GetString()?.Trim().ToLowerInvariant() ?? string.Empty
                : string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                _logger.LogWarning("ResolveShiftStatus: Status input is missing.");
                return CreateError(call.Id, "Shift status is required.");
            }

            // Map known synonyms to the enum name
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
                return CreateError(call.Id, $"Invalid shift status: '{input}'. Expected one of: Scheduled, Assigned, Completed, Cancelled, Vacant.");
            }

            // Parse enum safely
            if (!Enum.TryParse(typeof(ShiftStatuses), matchedStatus, out var enumValue))
            {
                _logger.LogError("ResolveShiftStatus: Could not parse matched status '{MatchedStatus}' into ShiftStatuses enum.", matchedStatus);
                return CreateError(call.Id, $"Internal error: Unable to parse status '{matchedStatus}'.");
            }

            var result = new
            {
                success = true,
                match = new
                {
                    input,
                    matchedStatus,
                    matchedStatusValue = (int)enumValue!
                }
            };

            _logger.LogInformation("ResolveShiftStatus: Mapped '{Input}' to '{MatchedStatus}'", input, matchedStatus);

            // Simulate async — in case future DB/API calls are added
            await Task.Yield();

            return new ToolOutput(call.Id, JsonSerializer.Serialize(result));
        }

        private ToolOutput CreateError(string callId, string message)
        {
            var error = new { success = false, error = message };
            return new ToolOutput(callId, JsonSerializer.Serialize(error));
        }
    }
}
