using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.Tools;
using HospitalSchedulingApp.Agent.Tools.HelperTools;
using HospitalSchedulingApp.Services.AuthServices.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.HelperHandlers
{
    public class ResolveLoggedInUserRoleToolHandler : IToolHandler
    {
        private readonly IUserContextService _userContextService;
        private readonly ILogger<ResolveLoggedInUserRoleToolHandler> _logger;

        public ResolveLoggedInUserRoleToolHandler(
            IUserContextService userContextService,

            ILogger<ResolveLoggedInUserRoleToolHandler> logger)
        {
            _userContextService = userContextService;
            _logger = logger;
        }

        public string ToolName => ResolveLoggedInUserRoleTool.GetTool().Name;

        public async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            try
            {
                var role = _userContextService.GetRole();

                if (role == null)
                {
                    _logger.LogWarning("ResolveLoggedInUserRole: No role found for the current user.");
                    return CreateError(call.Id, "User role could not be determined.");
                }

                _logger.LogInformation("ResolveLoggedInUserRole: Resolved role '{Role}'", role);

                var result = new
                {
                    success = true,
                    role = role                  
                };

                return new ToolOutput(call.Id, JsonSerializer.Serialize(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ResolveLoggedInUserRole: Error occurred while resolving user role.");
                return CreateError(call.Id, "An unexpected error occurred while resolving user role.");
            }
        }

        private ToolOutput CreateError(string callId, string message)
        {
            var error = new { success = false, error = message };
            return new ToolOutput(callId, JsonSerializer.Serialize(error));
        }
    }
}
