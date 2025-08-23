using Azure.AI.Agents.Persistent;
using Castle.Core.Logging;
using HospitalSchedulingApp.Agent.MetaResolver;
using HospitalSchedulingApp.Agent.Tools.HelperTools;
using HospitalSchedulingApp.Common.Extensions;
using HospitalSchedulingApp.Common.Handlers;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.HelperHandlers
{
    public class ResolveShiftTypeToolHandler : BaseToolHandler
    {
        private readonly IEntityResolver _entityResolver;

        public ResolveShiftTypeToolHandler(
            IEntityResolver entityResolver,
            ILogger<ResolveShiftTypeToolHandler> logger)
            : base(logger)
        {
            _entityResolver = entityResolver ?? throw new ArgumentNullException(nameof(entityResolver));
        }

        public override string ToolName => ResolveShiftTypeTool.GetTool().Name;

        public override async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            string input = root.FetchString("shift")?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                _logger.LogWarning("ResolveShiftType: Shift input is missing.");
                return CreateError(call.Id, "❌ Shift type is required.");
            }

            var resolved = await _entityResolver.ResolveEntitiesAsync(input);

            if (resolved.ShiftType == null)
            {
                _logger.LogInformation("ResolveShiftType: No shift type match found for '{Input}'", input);
                return CreateError(call.Id, $"❌ Invalid shift type: '{input}'. Please provide a valid shift type.");
            }

            var matchedTypeName = resolved.ShiftType.ShiftTypeName;
            var matchedTypeValue = resolved.ShiftType.ShiftTypeId;

            var data = new
            {
                input,
                matchedType = matchedTypeName,
                matchedTypeValue
            };

            _logger.LogInformation("ResolveShiftType: Mapped '{Input}' to ShiftType '{MatchedType}'", input, matchedTypeName);

            return CreateSuccess(call.Id, "✅ Resolved shift type successfully.", data);
        }
    }
}
