using Azure.AI.Agents.Persistent;
using Castle.Core.Logging;
using HospitalSchedulingApp.Agent.Tools.HelperTools;
using HospitalSchedulingApp.Common.Extensions;
using HospitalSchedulingApp.Common.Handlers;
using HospitalSchedulingApp.Services.AuthServices.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.HelperHandlers
{
    public class ResolveStaffReferenceToolHandler : BaseToolHandler
    {
        private readonly IUserContextService _userContextService;

        public ResolveStaffReferenceToolHandler(
            ILogger<ResolveStaffReferenceToolHandler> logger,
            IUserContextService userContextService)
            : base(logger)
        {
            _userContextService = userContextService ?? throw new ArgumentNullException(nameof(userContextService));
        }

        public override string ToolName => ResolveStaffReferenceTool.GetTool().Name;

        public override Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            try
            {
                string phrase = root.FetchString("phrase")?.ToLowerInvariant().Trim() ?? string.Empty;
                var staffId = _userContextService.GetStaffId();

                if (string.IsNullOrWhiteSpace(phrase))
                {
                    _logger.LogWarning("ResolveStaffReference: Missing or empty phrase.");
                    return Task.FromResult(CreateError(call.Id, "❌ A phrase is required to resolve staff reference."));
                }

                // 🔎 Check for self-reference
                bool isSelfReference = phrase.Contains("me") || phrase.Contains("my") || phrase.Contains(" i ");

                if (isSelfReference)
                {
                    var data = new
                    {
                        staffId,
                        isSelf = true
                    };

                    _logger.LogInformation("ResolveStaffReference: Detected self-reference for staffId={StaffId}", staffId);
                    return Task.FromResult(CreateSuccess(call.Id, "✅ Resolved to current user.", data));
                }

                // Fallback: no self-reference
                var fallbackData = new
                {
                    originalPhrase = phrase,
                    isSelf = false
                };

                _logger.LogInformation("ResolveStaffReference: No self-reference detected. Returning original phrase '{Phrase}'", phrase);
                return Task.FromResult(CreateSuccess(call.Id, "ℹ️ Resolved phrase without self-reference.", fallbackData));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❗ ResolveStaffReference: Unexpected error while resolving staff reference.");
                return Task.FromResult(CreateError(call.Id, "⚠️ An unexpected error occurred while resolving staff reference."));
            }
        }
    }
}
