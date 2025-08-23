using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.Tools.Staff;
using HospitalSchedulingApp.Common.Exceptions;
using HospitalSchedulingApp.Common.Extensions;
using HospitalSchedulingApp.Common.Handlers;
using HospitalSchedulingApp.Dtos.Staff.Requests;
using HospitalSchedulingApp.Services.AuthServices.Interfaces;
using HospitalSchedulingApp.Services.Interfaces;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.Staff
{
    /// <summary>
    /// Handles the <c>searchAvailableStaff</c> tool.
    /// Allows schedulers to search for available staff with optional filters and fatigue rules.
    /// </summary>
    public class SearchAvailableStaffToolHandler : BaseToolHandler
    {
        private readonly IStaffService _staffService;
        private readonly IUserContextService _userContextService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchAvailableStaffToolHandler"/> class.
        /// </summary>
        /// <param name="staffService">Service for fetching staff availability.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="userContextService">Service for accessing current user context.</param>
        public SearchAvailableStaffToolHandler(
            IStaffService staffService,
            ILogger<SearchAvailableStaffToolHandler> logger,
            IUserContextService userContextService)
            : base(logger)
        {
            _staffService = staffService;
            _userContextService = userContextService;
        }

        /// <inheritdoc/>
        public override string ToolName => SearchAvailableStaffTool.GetTool().Name;

        /// <summary>
        /// Handles execution of the <c>searchAvailableStaff</c> tool.
        /// Validates input, enforces authorization, applies filters,
        /// and queries the staff service for availability.
        /// </summary>
        /// <param name="call">The tool call metadata.</param>
        /// <param name="root">The input payload as JSON.</param>
        /// <returns> 
        public override async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            // 🔒 Ensure only schedulers can search staff
            var isScheduler = _userContextService.IsScheduler();
            if (!isScheduler)
            {
                _logger.LogWarning("Unauthorized access attempt to searchAvailableStaff by a non-scheduler.");
                return CreateError(call.Id, "🚫 You’re not authorized to perform this action.");
            }

            try
            {
                // 📅 Parse and validate date range
                DateTime? startDateTime = root.FetchDateTime("startDate");
                DateTime? endDateTime = root.FetchDateTime("endDate");

                if (startDateTime is null || endDateTime is null)
                {
                    return CreateError(call.Id, "❌ `startDate` and `endDate` are required in yyyy-MM-dd format.");
                }

                var startDate = DateOnly.FromDateTime(startDateTime.Value);
                var endDate = DateOnly.FromDateTime(endDateTime.Value);

                if (startDate > endDate)
                {
                    return CreateError(call.Id, "⚠️ Start date must be before or equal to end date.");
                }

                // 🎯 Build filter DTO
                var filterDto = new AvailableStaffFilterDto
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    ShiftTypeId = root.FetchInt("shiftTypeId"),
                    DepartmentId = root.FetchInt("departmentId"),
                    ApplyFatigueCheck = root.FetchBool("applyFatigueCheck") ?? true
                };

                _logger.LogInformation("Searching staff: {@Filter}", filterDto);

                // 🔍 Execute service call
                var dateWiseResult = await _staffService.SearchAvailableStaffAsync(filterDto);

                if (dateWiseResult == null || dateWiseResult.All(r => r?.AvailableStaff.Count == 0))
                {
                    _logger.LogInformation("No available staff found. FatigueCheck={ApplyFatigueCheck}", filterDto.ApplyFatigueCheck);

                    if (filterDto.ApplyFatigueCheck)
                    {
                        // 💡 Suggest retry with relaxed fatigue rules
                        var followUpPrompt = new
                        {
                            success = false,
                            prompt = "😕 No suitable staff found due to fatigue constraints. " +
                                     "Would you like me to retry by relaxing rest rules (e.g., allow back-to-back shifts)?",
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

                    return CreateError(call.Id, "No available staff found even after relaxing fatigue rules.");
                }

                // ✅ Success response
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
                                score = s.Score,
                                reasoning = s.Reasoning,
                                isFatigueRisk = s.IsFatigueRisk,
                                isCrossDepartment = s.IsCrossDepartment
                            })
                        )
                };

                return CreateSuccess(call.Id, "✅ Staff availability fetched successfully", output);
            }
            catch (BusinessRuleException ex)
            {
                return CreateError(call.Id, ex.Message);
            }
            catch (Exception)
            {
                return CreateError(call.Id, "❌ An error occurred while searching for available staff.");
            }
        }
    }
}
