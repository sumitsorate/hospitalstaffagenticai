using Azure.AI.Agents.Persistent;
using Castle.Core.Logging;
using HospitalSchedulingApp.Agent.Tools.HelperTools;
using HospitalSchedulingApp.Common.Extensions;
using HospitalSchedulingApp.Common.Handlers;
using HospitalSchedulingApp.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.HelperHandlers
{
    public class ResolveStaffInfoByNameToolHandler : BaseToolHandler
    {
        private readonly IStaffService _staffService;

        public ResolveStaffInfoByNameToolHandler(
            IStaffService staffService,
            ILogger<ResolveStaffInfoByNameToolHandler> logger)
            : base(logger)
        {
            _staffService = staffService ?? throw new ArgumentNullException(nameof(staffService));
        }

        public override string ToolName => ResolveStaffInfoByNameTool.GetTool().Name;

        public override async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            string inputName = root.FetchString("name")?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(inputName))
            {
                _logger.LogWarning("ResolveStaffInfoByName: Name input is missing.");
                return CreateError(call.Id, "❌ Staff name is required.");
            }

            if (inputName.Length < 2)
            {
                _logger.LogWarning("ResolveStaffInfoByName: Name '{Input}' is too short.", inputName);
                return CreateError(call.Id, "⚠️ Staff name must be at least 2 characters long.");
            }

            var matches = await _staffService.FetchActiveStaffByNamePatternAsync(inputName);

            if (!matches.Any())
            {
                _logger.LogInformation("ResolveStaffInfoByName: No staff found for '{Name}'", inputName);
                return CreateError(call.Id, $"❌ No staff found matching: {inputName}");
            }

            var data = new
            {
                matches
            };

            _logger.LogInformation("ResolveStaffInfoByName: Found {Count} match(es) for '{Input}'", matches.Count, inputName);

            return CreateSuccess(call.Id, "✅ Staff information resolved successfully.", data);
        }
    }
}
