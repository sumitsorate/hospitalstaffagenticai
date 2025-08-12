using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.MetaResolver;
using HospitalSchedulingApp.Agent.Tools.HelperTools;
using HospitalSchedulingApp.Common.Enums;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.HelperHandlers
{
    public class ResolveLeaveTypeToolHandler : IToolHandler
    {
        private readonly ILogger<ResolveLeaveTypeToolHandler> _logger;
        private readonly IEntityResolver _entityResolver;

        public ResolveLeaveTypeToolHandler(
            IEntityResolver entityResolver,
            ILogger<ResolveLeaveTypeToolHandler> logger)
        {
            _entityResolver = entityResolver;
            _logger = logger;
        }

        public string ToolName => ResolveLeaveTypeTool.GetTool().Name;

        public async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            string input = root.TryGetProperty("leaveType", out var leaveProp)
                ? leaveProp.GetString()?.Trim() ?? string.Empty
                : string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                _logger.LogWarning("ResolveLeaveType: Leave input is missing.");
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
                success = true,
                match = new
                {
                    input,
                    matchedType = matchedTypeName,
                    matchedTypeValue
                }
            };

            _logger.LogInformation("ResolveLeaveType: Mapped '{Input}' to '{MatchedType}'", input, matchedTypeName);
            return new ToolOutput(call.Id, JsonSerializer.Serialize(result));
        }

        private ToolOutput CreateError(string callId, string message)
        {
            var error = new { success = false, error = message };
            return new ToolOutput(callId, JsonSerializer.Serialize(error));
        }
    }
}
