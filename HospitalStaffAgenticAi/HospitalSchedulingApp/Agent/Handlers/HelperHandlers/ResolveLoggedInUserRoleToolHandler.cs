using Azure.AI.Agents.Persistent;
using Castle.Core.Logging;
using HospitalSchedulingApp.Agent.Tools.HelperTools;
using HospitalSchedulingApp.Common.Handlers;
using HospitalSchedulingApp.Services.AuthServices.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.HelperHandlers
{
    public class ResolveLoggedInUserRoleToolHandler : BaseToolHandler
    {
        private readonly IUserContextService _userContextService;

        public ResolveLoggedInUserRoleToolHandler(
            IUserContextService userContextService,
            ILogger<ResolveLoggedInUserRoleToolHandler> logger)
            : base(logger) // ✅ BaseToolHandler provides common logging + error helpers
        {
            _userContextService = userContextService;
        }

        public override string ToolName => ResolveLoggedInUserRoleTool.GetTool().Name;

        public override async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            try
            {
                var role = _userContextService.GetRole();

                if (string.IsNullOrWhiteSpace(role))
                {
                    _logger.LogWarning("ResolveLoggedInUserRole: No role found for the current user.");
                    return CreateError(call.Id, "User role could not be determined.");
                }

                _logger.LogInformation("ResolveLoggedInUserRole: Resolved role '{Role}'", role);

                var result = new
                {
                    role
                };

                return CreateSuccess(call.Id, "✅ User role resolved successfully.", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❗ ResolveLoggedInUserRole: Error occurred while resolving user role.");
                return CreateError(call.Id, "⚠️ An unexpected error occurred while resolving user role.");
            }
        }
    }
}
