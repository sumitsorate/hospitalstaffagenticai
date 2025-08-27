using Azure.AI.Agents.Persistent;
using Castle.Core.Logging;
using HospitalSchedulingApp.Agent.MetaResolver;
using HospitalSchedulingApp.Agent.Tools.HelperTools;
using HospitalSchedulingApp.Common.Enums;
using HospitalSchedulingApp.Common.Extensions;
using HospitalSchedulingApp.Common.Handlers;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.LeaveRequest
{
    public class ResolveLeaveStatusToolHandler : BaseToolHandler
    {
        private readonly IEntityResolver _entityResolver;

        public ResolveLeaveStatusToolHandler(
            IEntityResolver entityResolver,
            ILogger<ResolveLeaveStatusToolHandler> logger)
            : base(logger) // ✅ BaseToolHandler takes logger
        {
            _entityResolver = entityResolver;
        }

        public override string ToolName => ResolveLeaveStatusTool.GetTool().Name;

        public override async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            try
            {
                string input = root.FetchString("status")?.Trim() ?? string.Empty;

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

                return CreateSuccess(call.Id, "✅ Leave status resolved successfully.", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❗ Error in ResolveLeaveStatusToolHandler");
                return CreateError(call.Id, "⚠️ An internal error occurred while resolving leave status.");
            }
        }
    }
}
