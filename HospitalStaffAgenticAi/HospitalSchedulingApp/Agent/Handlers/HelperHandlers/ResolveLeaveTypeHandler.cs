using Azure.AI.Agents.Persistent;
using Castle.Core.Logging;
using HospitalSchedulingApp.Agent.MetaResolver;
using HospitalSchedulingApp.Agent.Tools.HelperTools;
using HospitalSchedulingApp.Common.Enums;
using HospitalSchedulingApp.Common.Extensions;
using HospitalSchedulingApp.Common.Handlers;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.HelperHandlers
{
    public class ResolveLeaveTypeToolHandler : BaseToolHandler
    {
        private readonly IEntityResolver _entityResolver;

        public ResolveLeaveTypeToolHandler(
            IEntityResolver entityResolver,
            ILogger<ResolveLeaveTypeToolHandler> logger)
            : base(logger) // ✅ BaseToolHandler takes logger
        {
            _entityResolver = entityResolver;
        }

        public override string ToolName => ResolveLeaveTypeTool.GetTool().Name;

        public override async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            try
            {
                string input = root.FetchString("leaveType")?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(input))
                {
                    _logger.LogWarning("ResolveLeaveType: Leave type input is missing.");
                    return CreateError(call.Id, "Leave type is required.");
                }

                // Use EntityResolver to resolve entities including LeaveType
                var resolveResult = await _entityResolver.ResolveEntitiesAsync(input);
                var leaveTypeEntity = resolveResult.LeaveType;

                if (leaveTypeEntity == null)
                {
                    _logger.LogInformation("ResolveLeaveType: No matching leave type found for input '{Input}'", input);
                    var validTypes = string.Join(", ", Enum.GetNames(typeof(LeaveType)));
                    return CreateError(call.Id, $"Invalid leave type: '{input}'. Valid types are: {validTypes}.");
                }

                var matchedTypeName = leaveTypeEntity.LeaveTypeName;
                var matchedTypeValue = (int)Enum.Parse(typeof(LeaveType), matchedTypeName);

                var result = new
                {
                    input,
                    matchedType = matchedTypeName,
                    matchedTypeValue
                };

                _logger.LogInformation("ResolveLeaveType: Mapped '{Input}' to '{MatchedType}'", input, matchedTypeName);

                return CreateSuccess(call.Id, "✅ Leave type resolved successfully.", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❗ Error in ResolveLeaveTypeToolHandler");
                return CreateError(call.Id, "⚠️ An internal error occurred while resolving leave type.");
            }
        }
    }
}
