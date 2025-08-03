using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.Tools.HelperTools;
using HospitalSchedulingApp.Services.AuthServices.Interfaces;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.HelperHandlers
{
    public class ResolveStaffReferenceToolHandler : IToolHandler
    {
        private readonly ILogger<ResolveStaffReferenceToolHandler> _logger;
        private readonly IUserContextService _userContextService;

        public ResolveStaffReferenceToolHandler(
            ILogger<ResolveStaffReferenceToolHandler> logger,
            IUserContextService userContextService)
        {
            _logger = logger;
            _userContextService = userContextService;
        }

        public string ToolName => ResolveStaffReferenceTool.GetTool().Name;

        public Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            try
            {
                if (!root.TryGetProperty("phrase", out var phraseElement))
                {
                    _logger.LogWarning("Missing 'phrase' parameter in resolveStaffReference tool call.");
                    return Task.FromResult<ToolOutput?>(null);
                }

                var phrase = phraseElement.GetString()?.ToLowerInvariant().Trim() ?? string.Empty;
                var staffId = _userContextService.GetStaffId();

                // Check for self-reference
                bool isSelfReference = phrase.Contains("me") || phrase.Contains("my") || phrase.Contains(" i ");

                if (isSelfReference)
                {
                    var resultJson = JsonSerializer.Serialize(new
                    {
                        staffId = staffId,
                        isSelf = true
                    });

                    return Task.FromResult<ToolOutput?>(new ToolOutput(call.Id, resultJson));
                }

                // If no self-reference, just return phrase unchanged (or optionally null)
                var fallbackResult = JsonSerializer.Serialize(new
                {
                    originalPhrase = phrase,
                    isSelf = false
                });

                return Task.FromResult<ToolOutput?>(new ToolOutput(call.Id, fallbackResult));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving staff reference.");
                return Task.FromResult<ToolOutput?>(null);
            }
        }
    }
}
