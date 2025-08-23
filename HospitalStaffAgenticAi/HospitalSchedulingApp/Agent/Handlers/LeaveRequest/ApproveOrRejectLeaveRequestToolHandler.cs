using Azure.AI.Agents.Persistent;
using Castle.Core.Logging;
using HospitalSchedulingApp.Agent.Tools.LeaveRequest;
using HospitalSchedulingApp.Common.Enums;
using HospitalSchedulingApp.Common.Exceptions;
using HospitalSchedulingApp.Common.Extensions;
using HospitalSchedulingApp.Common.Handlers;
using HospitalSchedulingApp.Dtos.LeaveRequest.Request;
using HospitalSchedulingApp.Dtos.Shift.Requests;
using HospitalSchedulingApp.Dtos.Shift.Response;
using HospitalSchedulingApp.Dtos.Staff.Requests;
using HospitalSchedulingApp.Services.AuthServices.Interfaces;
using HospitalSchedulingApp.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.LeaveRequest
{
    public class ApproveOrRejectLeaveRequestToolHandler : BaseToolHandler
    {
        private readonly ILeaveRequestService _leaveRequestService;
        private readonly IPlannedShiftService _plannedShiftService;
        private readonly IStaffService _staffService;
        private readonly IUserContextService _userContextService;

        public ApproveOrRejectLeaveRequestToolHandler(
            ILeaveRequestService leaveRequestService,
            ILogger<ApproveOrRejectLeaveRequestToolHandler> logger,
            IPlannedShiftService plannedShiftService,
            IUserContextService userContextService,
            IStaffService staffService)
            : base(logger)
        {
            _leaveRequestService = leaveRequestService;
            _plannedShiftService = plannedShiftService;
            _staffService = staffService;
            _userContextService = userContextService;
        }

        public override string ToolName => ApproveOrRejectLeaveRequestTool.GetTool().Name;

        public override async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {

            try
            {
                // Extract inputs
                int? leaveRequestId = root.FetchInt("leaveRequestId");
                int? staffId = root.FetchInt("staffId");
                int? leaveTypeId = root.FetchInt("leaveTypeId");

                DateTime? startDate = root.FetchDateTime("startDate");
                DateTime? endDate = root.FetchDateTime("endDate");

                // Validate status
                string? statusStr = root.FetchString("newStatus");
                if (string.IsNullOrWhiteSpace(statusStr) ||
                    !Enum.TryParse<LeaveRequestStatuses>(statusStr, true, out var newStatus) ||
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

                // Collect impacted shifts if Approved
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
                        // Unassign shift
                        await _plannedShiftService.UnassignedShiftFromStaffAsync(impactedShift.PlannedShiftId);

                        var availableStaffFilter = new AvailableStaffFilterDto
                        {
                            StartDate = DateOnly.FromDateTime(impactedShift.ShiftDate),
                            ShiftTypeId = impactedShift.ShiftTypeId,
                            DepartmentId = impactedShift.DepartmentId,
                            EndDate = DateOnly.FromDateTime(impactedShift.ShiftDate)
                        };

                        // Suggest replacements
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

                return CreateSuccess(call.Id,
                    $"✅ Leave request has been successfully **{newStatus.ToString().ToLower()}**.",
                    new
                    {
                        leaveRequest = updatedRequest,
                        impactedShifts = shiftReplacementOptions
                    });
            }
            catch (BusinessRuleException ex)
            {
                _logger.LogError(ex, ex.Message);
                return CreateError(call.Id, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ApproveOrRejectLeaveRequestToolHandler");
                return CreateError(call.Id, "❌ An internal error occurred while processing the leave request.");
            }
        }
    }
}
