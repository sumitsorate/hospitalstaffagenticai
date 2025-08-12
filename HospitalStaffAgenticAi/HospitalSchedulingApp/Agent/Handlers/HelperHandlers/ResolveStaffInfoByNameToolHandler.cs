using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.MetaResolver;
using HospitalSchedulingApp.Agent.Tools.HelperTools;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.HelperHandlers
{
    public class ResolveStaffInfoByNameToolHandler : IToolHandler
    {
        private readonly IEntityResolver _entityResolver;
        private readonly ILogger<ResolveStaffInfoByNameToolHandler> _logger;

        public ResolveStaffInfoByNameToolHandler(
            IEntityResolver entityResolver,
            ILogger<ResolveStaffInfoByNameToolHandler> logger)
        {
            _entityResolver = entityResolver;
            _logger = logger;
        }

        public string ToolName => ResolveStaffInfoByNameTool.GetTool().Name;

        public async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            string inputName = root.TryGetProperty("name", out var nameProp)
                ? nameProp.GetString()?.Trim() ?? string.Empty
                : string.Empty;

            if (string.IsNullOrWhiteSpace(inputName))
            {
                _logger.LogWarning("resolveStaffInfoByName: Name was not provided.");
                return CreateError(call.Id, "Staff name is required.");
            }

            if (inputName.Length < 2)
            {
                _logger.LogWarning("resolveStaffInfoByName: Name '{Input}' is too short.", inputName);
                return CreateError(call.Id, "Staff name must be at least 2 characters long.");
            }

            var resolved = await _entityResolver.ResolveEntitiesAsync(inputName);

            if (resolved.Staff == null)
            {
                _logger.LogInformation("resolveStaffInfoByName: No staff found for '{Name}'", inputName);
                return CreateError(call.Id, $"No staff found matching: {inputName}");
            }

            var result = new
            {
                success = true,
                matches = new[]
                {
                    new
                    {
                        staffId = resolved.Staff.StaffId,
                        staffName = resolved.Staff.StaffName,
                        roleId = resolved.Staff.RoleId,                     
                        departmentId = resolved.Staff.StaffDepartmentId                       
                    }
                }
            };

            _logger.LogInformation("resolveStaffInfoByName: Found 1 match for '{Input}'", inputName);
            return new ToolOutput(call.Id, JsonSerializer.Serialize(result));
        }

        private ToolOutput CreateError(string callId, string message)
        {
            var errorJson = JsonSerializer.Serialize(new
            {
                success = false,
                error = message
            });

            return new ToolOutput(callId, errorJson);
        }
    }
}
