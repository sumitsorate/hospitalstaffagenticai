using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.Tools.Department;
using HospitalSchedulingApp.Common.Exceptions; // ✅ Assuming BusinessRuleException lives here
using HospitalSchedulingApp.Common.Extensions;
using HospitalSchedulingApp.Common.Handlers;
using HospitalSchedulingApp.Services.Interfaces;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers
{
    /// <summary>
    /// 🏥 Tool handler for resolving department information by name.
    /// Uses <see cref="IDepartmentService"/> to fetch matching department details.
    /// </summary>
    public class ResolveDepartmentInfoToolHandler : BaseToolHandler
    {
        private readonly IDepartmentService _departmentService;

        /// <summary>
        /// Initializes a new instance of <see cref="ResolveDepartmentInfoToolHandler"/>.
        /// </summary>
        /// <param name="departmentService">Service for department operations.</param>
        /// <param name="logger">Logger for structured logging.</param>
        public ResolveDepartmentInfoToolHandler(
            IDepartmentService departmentService,
            ILogger<ResolveDepartmentInfoToolHandler> logger)
            : base(logger)
        {
            _departmentService = departmentService;
        }

        /// <summary>
        /// Gets the tool name registered in the agent runtime.
        /// </summary>
        public override string ToolName => ResolveDepartmentInfoTool.GetTool().Name;

        /// <summary>
        /// Handles a tool call for resolving department info by name.
        /// </summary>
        /// <param name="call">Tool call metadata.</param>
        /// <param name="root">Input JSON payload.</param>
        /// <returns>Tool output containing department details or error message.</returns>
        public override async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            try
            {
                // 🔍 Extract and validate department name
                string inputName = root.FetchString("name")?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(inputName))
                {
                    _logger.LogWarning("resolveDepartmentInfo: Missing input parameter 'name'.");
                    return CreateError(call.Id, "❌ Department name is required.");
                }

                if (inputName.Length < 2)
                {
                    _logger.LogWarning("resolveDepartmentInfo: Input '{Input}' is too short.", inputName);
                    return CreateError(call.Id, "⚠️ Department name must be at least 2 characters long.");
                }

                _logger.LogInformation("resolveDepartmentInfo: Resolving department for input '{Input}'...", inputName);

                // 🏥 Resolve department info from service
                var resolved = await _departmentService.FetchDepartmentInformationAsync(inputName);

                if (resolved == null)
                {
                    _logger.LogInformation("resolveDepartmentInfo: No department found for input '{Input}'", inputName);
                    return CreateError(call.Id, $"⚠️ No department found matching: {inputName}");
                }

                // ✅ Build success response
                var result = new
                {
                    success = true,
                    department = new
                    {
                        resolved.DepartmentId,
                        resolved.DepartmentName
                    }
                };

                _logger.LogInformation(
                    "resolveDepartmentInfo: Successfully matched '{Input}' to Department ID {Id} - {Name}",
                    inputName, resolved.DepartmentId, resolved.DepartmentName);

                return CreateSuccess(call.Id, "✅ Department resolved successfully.", result);
            }
            catch (BusinessRuleException brex)
            {
                // ⚖️ Business rule violation
                _logger.LogWarning(brex, "resolveDepartmentInfo: Business rule exception occurred.");
                return CreateError(call.Id, $"⚠️ {brex.Message}");
            }
            catch (Exception ex)
            {
                // ❌ Unexpected system failure
                _logger.LogError(ex, "resolveDepartmentInfo: Unexpected error occurred.");
                return CreateError(call.Id, "❌ An internal error occurred while resolving department info.");
            }
        }
    }
}
