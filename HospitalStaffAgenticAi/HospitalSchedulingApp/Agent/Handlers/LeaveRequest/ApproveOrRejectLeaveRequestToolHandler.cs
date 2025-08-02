using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.Tools.LeaveRequest;
using HospitalSchedulingApp.Common.Enums;
using HospitalSchedulingApp.Dtos.LeaveRequest.Request;
using HospitalSchedulingApp.Dtos.LeaveRequest.Response;
using HospitalSchedulingApp.Dtos.Shift.Requests;
using HospitalSchedulingApp.Dtos.Shift.Response;
using HospitalSchedulingApp.Dtos.Staff.Requests;
using HospitalSchedulingApp.Dtos.Staff.Response;
using HospitalSchedulingApp.Services.AuthServices.Interfaces;
using HospitalSchedulingApp.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.LeaveRequest
{
    public class ApproveOrRejectLeaveRequestToolHandler : IToolHandler
    {
        private readonly ILeaveRequestService _leaveRequestService;
        private readonly IPlannedShiftService _plannedShiftService;
        private readonly IStaffService _staffService;
        private readonly ILogger<ApproveOrRejectLeaveRequestToolHandler> _logger;
        private readonly IUserContextService _userContextService;

        public ApproveOrRejectLeaveRequestToolHandler(
            ILeaveRequestService leaveRequestService,
            ILogger<ApproveOrRejectLeaveRequestToolHandler> logger,
            IPlannedShiftService plannedShiftService,
            IUserContextService userContextService,
            IStaffService staffService)
        {
            _leaveRequestService = leaveRequestService;
            _logger = logger;
            _plannedShiftService = plannedShiftService;
            _staffService = staffService;
            _userContextService = userContextService;
        }

        public string ToolName => ApproveOrRejectLeaveRequestTool.GetTool().Name;

        public async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            var isScheduler = _userContextService.IsScheduler();
            if (!isScheduler)
            {
                return CreateError(call.Id, "🚫 Oops! You're not authorized to perform this action. Let me know if you need help with something else.");
            }
            try
            {
                // Extract inputs
                int? leaveRequestId = root.TryGetProperty("leaveRequestId", out var idProp) && idProp.TryGetInt32(out var lid) ? lid : null;
                int? staffId = root.TryGetProperty("staffId", out var staffProp) && staffProp.TryGetInt32(out var sid) ? sid : null;
                int? leaveTypeId = root.TryGetProperty("leaveTypeId", out var typeProp) && typeProp.TryGetInt32(out var ltid) ? ltid : null;

                DateTime? startDate = root.TryGetProperty("startDate", out var startProp) &&
                                      DateTime.TryParse(startProp.GetString(), out var start)
                                      ? start : null;

                DateTime? endDate = root.TryGetProperty("endDate", out var endProp) &&
                                    DateTime.TryParse(endProp.GetString(), out var end)
                                    ? end : null;

                // Validate status
                if (!root.TryGetProperty("newStatus", out var statusProp) ||
                    !Enum.TryParse<LeaveRequestStatuses>(statusProp.GetString(), true, out var newStatus) ||
                    (newStatus != LeaveRequestStatuses.Approved && newStatus != LeaveRequestStatuses.Rejected))
                {
                    return CreateError(call.Id, "❌ Invalid or missing `newStatus`. It must be either 'Approved' or 'Rejected'.");
                }

                // Filter leave requests
                var leaveRequestFilter = new LeaveRequestFilter
                {
                    LeaveRequestId = leaveRequestId,
                    StaffId = staffId,
                    StartDate = startDate,
                    EndDate = endDate,
                    LeaveStatusId = LeaveRequestStatuses.Pending
                };

                var matchingRequests = await _leaveRequestService.FetchLeaveRequestsAsync(leaveRequestFilter);
                if (matchingRequests == null || !matchingRequests.Any())
                {
                    return CreateError(call.Id, "❌ No matching pending leave request found.");
                }

                var requestToUpdate = matchingRequests.First();
                var updatedRequest = await _leaveRequestService.UpdateStatusAsync(requestToUpdate.LeaveRequestId, newStatus);
                if (updatedRequest == null)
                {
                    return CreateError(call.Id, "❌ Failed to update leave request. It may already be processed or does not exist.");
                }

                // Only do shift replacement suggestion if approved
                var shiftReplacementOptions = new List<ShiftReplacementOptions>();

                if (newStatus == LeaveRequestStatuses.Approved)
                {
                    var shiftFilter = new ShiftFilterDto
                    {
                        StaffId = updatedRequest.StaffId,
                        FromDate = updatedRequest.LeaveStart,
                        ToDate = updatedRequest.LeaveEnd
                    };

                    var impactedShifts = await _plannedShiftService.FetchFilteredPlannedShiftsAsync(shiftFilter);

                    foreach (var impactedShift in impactedShifts)
                    {
                        // Unassign Shifts
                        var unassignShift = await _plannedShiftService
                            .UnassignedShiftFromStaffAsync(impactedShift.PlannedShiftId);

                        var availableStaffFilter = new AvailableStaffFilterDto
                        {
                            StartDate = DateOnly.FromDateTime(impactedShift.ShiftDate),
                            ShiftTypeId = impactedShift.ShiftTypeId,
                            DepartmentId = impactedShift.DepartmentId,
                            EndDate = DateOnly.FromDateTime(impactedShift.ShiftDate)
                        };

                        // Replace shifts with staff
                        var replacements = await _staffService.SearchAvailableStaffAsync(availableStaffFilter);

                        shiftReplacementOptions.Add(new ShiftReplacementOptions
                        {
                            PlannedShiftId = impactedShift.PlannedShiftId,
                            DepartmentId = impactedShift.DepartmentId,
                            DepartmentName = impactedShift.ShiftDeparmentName,
                            ShiftDate = impactedShift.ShiftDate,
                            ShiftTypeId = impactedShift.ShiftTypeId,
                            ShiftTypeName = impactedShift.ShiftTypeName,
                            ShiftReplacements = replacements?.Take(3).ToList() ?? new()
                        });
                    }
                }

                var response = new
                {
                    success = true,
                    message = $"✅ Leave request has been successfully **{newStatus.ToString().ToLower()}**.",
                    leaveRequest = updatedRequest,
                    impactedShifts = shiftReplacementOptions
                };

                string json = JsonSerializer.Serialize(response);
                _logger.LogInformation("Leave request update result: {Json}", json);

                return new ToolOutput(call.Id, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ApproveOrRejectLeaveRequestToolHandler");
                return CreateError(call.Id, "❌ An internal error occurred while processing the leave request.");
            }
        }

        private ToolOutput CreateError(string toolCallId, string message)
        {
            var error = new
            {
                success = false,
                error = message
            };
            return new ToolOutput(toolCallId, JsonSerializer.Serialize(error));
        }
    }
}
