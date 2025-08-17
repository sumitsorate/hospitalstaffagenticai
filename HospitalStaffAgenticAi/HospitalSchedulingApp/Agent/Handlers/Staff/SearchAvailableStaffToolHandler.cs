using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.Tools.Staff;
using HospitalSchedulingApp.Dtos.Staff.Requests;
using HospitalSchedulingApp.Services.AuthServices.Interfaces;
using HospitalSchedulingApp.Services.Interfaces;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.Staff
{
    /// <summary>
    /// Handles the execution of the searchAvailableStaff tool for schedulers.
    /// Allows searching for available staff between a specified date range with optional department and shift type filters.
    /// </summary>
    public class SearchAvailableStaffToolHandler : IToolHandler
    {
        private readonly IStaffService _staffService;
        private readonly ILogger<SearchAvailableStaffToolHandler> _logger;
        private readonly IUserContextService _userContextService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchAvailableStaffToolHandler"/> class.
        /// </summary>
        public SearchAvailableStaffToolHandler(
            IStaffService staffService,
            ILogger<SearchAvailableStaffToolHandler> logger,
            IUserContextService userContextService)
        {
            _staffService = staffService;
            _logger = logger;
            _userContextService = userContextService;
        }

        /// <summary>
        /// Gets the tool name used by the agent runtime.
        /// </summary>
        public string ToolName => SearchAvailableStaffTool.GetTool().Name;

        /// <summary>
        /// Handles the tool call by validating input, verifying permissions, and returning available staff.
        /// </summary>
        /// <param name="call">The function tool call object.</param>
        /// <param name="root">The JSON input payload.</param>
        /// <returns>A structured <see cref="ToolOutput"/> containing available staff or an error.</returns>
        public async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            var isScheduler = _userContextService.IsScheduler();
            if (!isScheduler)
            {
                _logger.LogWarning("Unauthorized access attempt to searchAvailableStaff by a non-scheduler.");
                return CreateError(call.Id, "🚫 Oops! You're not authorized to perform this action. Let me know if you need help with something else.");
            }

            try
            {
                if (!root.TryGetProperty("startDate", out var startDateProp) ||
                    !root.TryGetProperty("endDate", out var endDateProp))
                {
                    _logger.LogWarning("Missing required date range parameters in searchAvailableStaff tool call.");
                    return CreateError(call.Id, "startDate and endDate are required.");
                }

                if (!DateOnly.TryParse(startDateProp.GetString(), out var startDate) ||
                    !DateOnly.TryParse(endDateProp.GetString(), out var endDate))
                {
                    _logger.LogWarning("Invalid date format received in searchAvailableStaff.");
                    return CreateError(call.Id, "Invalid date format for startDate or endDate.");
                }

                if (startDate > endDate)
                {
                    _logger.LogWarning("Invalid date range: Start date is after end date.");
                    return CreateError(call.Id, "Start date must be before or equal to end date.");
                }

                var filterDto = new AvailableStaffFilterDto
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    ShiftTypeId = root.TryGetProperty("shiftTypeId", out var shiftTypeProp) && shiftTypeProp.ValueKind == JsonValueKind.Number
                        ? shiftTypeProp.GetInt32()
                        : (int?)null,
                    DepartmentId = root.TryGetProperty("departmentId", out var deptProp) && deptProp.ValueKind == JsonValueKind.Number
                        ? deptProp.GetInt32()
                        : (int?)null,
                    ApplyFatigueCheck = root.TryGetProperty("applyFatigueCheck", out var fatigueProp) && fatigueProp.ValueKind == JsonValueKind.False
                        ? false
                        : true // default is true
                };

                _logger.LogInformation("Searching available staff: start={StartDate}, end={EndDate}, dept={DepartmentId}, shift={ShiftTypeId}",
                    filterDto.StartDate, filterDto.EndDate, filterDto.DepartmentId, filterDto.ShiftTypeId);

                var dateWiseResult = await _staffService.SearchAvailableStaffAsync(filterDto);

                //if (dateWiseResult == null || dateWiseResult.All(r => r?.AvailableStaff.Count == 0))
                //{
                //    _logger.LogInformation("searchAvailableStaff: No available staff found for given filter.");
                //    return CreateError(call.Id, "No available staff found for the given criteria.");
                //}
                if (dateWiseResult == null || dateWiseResult.All(r => r?.AvailableStaff.Count == 0))
                {
                    _logger.LogInformation("searchAvailableStaff: No available staff found for given filter. FatigueCheck={ApplyFatigueCheck}", filterDto.ApplyFatigueCheck);

                    if (filterDto.ApplyFatigueCheck)
                    {
                        // Suggest relaxing fatigue check to the user
                        var followUpPrompt = new
                        {
                            success = false,
                            prompt = "😕 No suitable staff found due to fatigue constraints. " +
                                     "Would you like me to retry the search by relaxing rest rules (e.g., allow back-to-back shifts)?",
                            suggestedNextAction = new
                            {
                                tool = ToolName,
                                modifiedPayload = new
                                {
                                    startDate = filterDto.StartDate.ToString("yyyy-MM-dd"),
                                    endDate = filterDto.EndDate.ToString("yyyy-MM-dd"),
                                    shiftTypeId = filterDto.ShiftTypeId,
                                    departmentId = filterDto.DepartmentId,
                                    applyFatigueCheck = false
                                }
                            }
                        };

                        return new ToolOutput(call.Id, JsonSerializer.Serialize(followUpPrompt));
                    }

                    // No staff even after fatigue relaxed
                    return CreateError(call.Id, "No available staff found even after relaxing fatigue rules.");
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
                                isActive = s.IsActive,
                                Score = s.Score,
                                Reasoning =s.Reasoning,
                                IsFatigueRisk = s.IsFatigueRisk,
                                IsCrossDepartment = s.IsCrossDepartment
                            })
                        )
                };

                return new ToolOutput(call.Id, JsonSerializer.Serialize(output));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in searchAvailableStaff tool handler.");
                return CreateError(call.Id, "An error occurred while processing the request.");
            }
        }

        /// <summary>
        /// Creates a structured error output for the tool call.
        /// </summary>
        /// <param name="callId">The tool call ID.</param>
        /// <param name="message">The error message to return.</param>
        /// <returns>A structured <see cref="ToolOutput"/> error response.</returns>
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
