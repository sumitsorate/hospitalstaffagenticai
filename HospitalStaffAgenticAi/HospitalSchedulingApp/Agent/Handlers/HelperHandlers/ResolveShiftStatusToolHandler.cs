using Azure.AI.Agents.Persistent;
using Castle.Core.Logging;
using HospitalSchedulingApp.Agent.MetaResolver;
using HospitalSchedulingApp.Agent.Tools.HelperTools;
using HospitalSchedulingApp.Common.Extensions;
using HospitalSchedulingApp.Common.Handlers;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.LeaveRequest
{
    public class ResolveShiftStatusToolHandler : BaseToolHandler
    {
        private readonly IEntityResolver _entityResolver;

        public ResolveShiftStatusToolHandler(
            IEntityResolver entityResolver,
            ILogger<ResolveShiftStatusToolHandler> logger)
            : base(logger)
        {
            _entityResolver = entityResolver ?? throw new ArgumentNullException(nameof(entityResolver));
        }

        public override string ToolName => ResolveShiftStatusTool.GetTool().Name;

        public override async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            if (call == null)
            {
                _logger.LogError("ResolveShiftStatusToolHandler: Tool call was null.");
                return CreateError(Guid.NewGuid().ToString(), "❌ Invalid tool call object.");
            }

            string input = root.FetchString("status")?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(input))
            {
                _logger.LogWarning("ResolveShiftStatus: Status input is missing.");
                return CreateError(call.Id, "❌ Shift status is required.");
            }

            // Resolve shift status entity
            var resolved = await _entityResolver.ResolveEntitiesAsync(input);
            if (resolved.ShiftStatus == null)
            {
                _logger.LogInformation("ResolveShiftStatus: No matching shift status found for '{Input}'", input);
                return CreateError(call.Id, $"❌ Invalid shift status: '{input}'. Please provide a valid status.");
            }

            var matchedStatusName = resolved.ShiftStatus.ShiftStatusName;
            var matchedStatusValue = resolved.ShiftStatus.ShiftStatusId;

            var data = new
            {
                input,
                matchedStatus = matchedStatusName,
                matchedStatusValue
            };

            _logger.LogInformation("ResolveShiftStatus: Mapped '{Input}' to '{MatchedStatus}'", input, matchedStatusName);

            return CreateSuccess(call.Id, "✅ Resolved shift status successfully.", data);
        }
    }
}
