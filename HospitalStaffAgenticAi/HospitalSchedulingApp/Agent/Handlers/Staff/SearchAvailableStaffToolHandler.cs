using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.Handlers;
using HospitalSchedulingApp.Agent.Tools.Staff;
using HospitalSchedulingApp.Dtos.Staff;
using HospitalSchedulingApp.Dtos.Staff.Requests;
using HospitalSchedulingApp.Services.AuthServices.Interfaces;
using HospitalSchedulingApp.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

public class SearchAvailableStaffToolHandler : IToolHandler
{
    private readonly IStaffService _staffService;
    private readonly ILogger<SearchAvailableStaffToolHandler> _logger;
    private readonly IUserContextService _userContextService;

    public SearchAvailableStaffToolHandler(
        IStaffService staffService,
        ILogger<SearchAvailableStaffToolHandler> logger,
        IUserContextService userContextService)
    {
        _staffService = staffService;
        _logger = logger;
        _userContextService = userContextService;
    }

    public string ToolName => SearchAvailableStaffTool.GetTool().Name;

    public async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
    {
        var isScheduler = _userContextService.IsScheduler();
        if (!isScheduler)
        {
            return CreateError(call.Id, "🚫 Oops! You're not authorized to perform this action. Let me know if you need help with something else.");
        }
        try
        {
            if (!root.TryGetProperty("startDate", out var startDateProp) ||
                !root.TryGetProperty("endDate", out var endDateProp))
            {
                return CreateError(call.Id, "startDate and endDate are required.");
            }

            var filterDto = new AvailableStaffFilterDto
            {
                StartDate = DateOnly.Parse(startDateProp.GetString()!),
                EndDate = DateOnly.Parse(endDateProp.GetString()!),
                ShiftTypeId = root.TryGetProperty("shiftTypeId", out var shiftTypeProp) && shiftTypeProp.ValueKind == JsonValueKind.Number
                    ? shiftTypeProp.GetInt32()
                    : (int?)null,
                DepartmentId = root.TryGetProperty("departmentId", out var deptProp) && deptProp.ValueKind == JsonValueKind.Number
                    ? deptProp.GetInt32()
                    : (int?)null
            };

            var dateWiseResult = await _staffService.SearchAvailableStaffAsync(filterDto);

            if (dateWiseResult == null || dateWiseResult.All(r => r?.AvailableStaff.Count == 0))
            {
                _logger.LogInformation("searchAvailableStaff: No available staff found for given filter.");
                return CreateError(call.Id, "No available staff found for the given criteria.");
            }

            var output = new
            {
                success = true,
                availableStaff = dateWiseResult
                    .Where(r => r != null && r.AvailableStaff.Count > 0)
                    .ToDictionary(
                        r => r!.Date.ToString("yyyy-MM-dd"),
                        r => r.AvailableStaff.Select(s => new
                        {
                            staffId = s.StaffId,
                            staffName = s.StaffName,
                            roleId = s.RoleId,
                            roleName = s.RoleName,
                            departmentId = s.StaffDepartmentId,
                            departmentName = s.StaffDepartmentName,
                            isActive = s.IsActive
                        })
                    )
            };

            return new ToolOutput(call.Id, JsonSerializer.Serialize(output));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in searchAvailableStaff tool handler.");
            return CreateError(call.Id, "An error occurred while processing the request.");
        }
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
