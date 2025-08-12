using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.MetaResolver;
using HospitalSchedulingApp.Agent.Tools.HelperTools;
using HospitalSchedulingApp.Common.Enums;
using HospitalSchedulingApp.Dal.Repositories;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.LeaveRequest
{
    public class ResolveLeaveStatusToolHandler : IToolHandler
    {
        private readonly ILogger<ResolveLeaveStatusToolHandler> _logger;
        private readonly IEntityResolver _entityResolver;

        public ResolveLeaveStatusToolHandler(
            IEntityResolver entityResolver,
            ILogger<ResolveLeaveStatusToolHandler> logger)
        {
            _entityResolver = entityResolver;
            _logger = logger;
        }

        public string ToolName => ResolveLeaveStatusTool.GetTool().Name;

        public async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            string input = root.TryGetProperty("status", out var statusProp)
                ? statusProp.GetString()?.Trim() ?? string.Empty
                : string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                _logger.LogWarning("ResolveLeaveStatus: Status input is missing.");
                return CreateError(call.Id, "Leave status is required.");
            }

            // Use EntityResolver to parse entities from input phrase
            var resolveResult = await _entityResolver.ResolveEntitiesAsync(input);

            var leaveStatus = resolveResult.LeaveStatus;

            if (leaveStatus == null)
            {
                _logger.LogInformation("ResolveLeaveStatus: No matching leave status found for input '{Input}'", input);
                return CreateError(call.Id, $"Invalid leave status: '{input}'");
            }

            var matchedStatusName = leaveStatus.LeaveStatusName;
            var matchedStatusValue = (int)Enum.Parse(typeof(LeaveRequestStatuses), matchedStatusName);

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

            _logger.LogInformation("ResolveLeaveStatus: Mapped '{Input}' to '{MatchedStatus}'", input, matchedStatusName);
            return new ToolOutput(call.Id, JsonSerializer.Serialize(result));
        }

        private ToolOutput CreateError(string callId, string message)
        {
            var error = new { success = false, error = message };
            return new ToolOutput(callId, JsonSerializer.Serialize(error));
        }
    }
}
