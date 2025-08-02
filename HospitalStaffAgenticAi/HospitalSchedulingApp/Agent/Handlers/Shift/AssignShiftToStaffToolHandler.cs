using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.Tools.LeaveRequest;
using HospitalSchedulingApp.Agent.Tools.Shift;
using HospitalSchedulingApp.Common.Enums;
using HospitalSchedulingApp.Dtos.LeaveRequest.Request;
using HospitalSchedulingApp.Dtos.Shift.Requests;
using HospitalSchedulingApp.Dtos.Shift.Response;
using HospitalSchedulingApp.Dtos.Staff.Requests;
using HospitalSchedulingApp.Services.Interfaces;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.Shift
{
    public class AssignShiftToStaffToolHandler : IToolHandler
    {
        private readonly IPlannedShiftService _plannedShiftService;
        private readonly ILeaveRequestService _leaveRequestService;
        private readonly ILogger<AssignShiftToStaffToolHandler> _logger;

        public AssignShiftToStaffToolHandler(
            IPlannedShiftService plannedShiftService,
            ILogger<AssignShiftToStaffToolHandler> logger,
            ILeaveRequestService leaveRequestService)
        {
            _plannedShiftService = plannedShiftService;
            _logger = logger;
            _leaveRequestService = leaveRequestService;
        }

        public string ToolName => AssignShiftToStaffTool.GetTool().Name;

        public async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            try
            {
                if (!root.TryGetProperty("plannedShiftId", out var shiftIdProp) || !shiftIdProp.TryGetInt32(out var plannedShiftId))
                    return CreateError(call.Id, "❌ `plannedShiftId` is required and must be a valid integer.");

                if (!root.TryGetProperty("staffId", out var staffIdProp) || !staffIdProp.TryGetInt32(out var staffId))
                    return CreateError(call.Id, "❌ `staffId` is required and must be a valid integer.");

                // Fetch the shift information
                var shiftFilter = new ShiftFilterDto { PlannedShiftId = plannedShiftId };
                var shiftInfo = await _plannedShiftService.FetchFilteredPlannedShiftsAsync(shiftFilter);

                if (shiftInfo == null || !shiftInfo.Any())
                    return CreateError(call.Id, "❌ Shift information not found.");

                var firstShift = shiftInfo.First();

                // Check if same staff is already assigned
                if (firstShift.AssignedStaffId == staffId)
                {
                    return CreateError(call.Id, "❌ The same staff member is already assigned to this shift.");
                }

                // Check if the shift is already assigned to someone else
                if (firstShift.AssignedStaffId.HasValue && firstShift.AssignedStaffId != staffId)
                {
                    return CreateError(call.Id, $"❌ Shift is already assigned to another staff member (ID {firstShift.AssignedStaffId}).");
                }

                // Check for leave overlap
                // Check for leave overlap (Pending or Approved)
                var overlappingLeaves = await _leaveRequestService.FetchLeaveRequestsAsync(new LeaveRequestFilter
                {
                    StaffId = staffId,
                    StartDate = firstShift.ShiftDate,
                    EndDate = firstShift.ShiftDate
                });

                if (overlappingLeaves?.Any(lr =>
                    lr.LeaveStatus == LeaveRequestStatuses.Approved ||
                    lr.LeaveStatus == LeaveRequestStatuses.Pending) == true)
                {
                    return CreateError(call.Id, $"❌ Staff ID {staffId} has a leave (pending/approved) on {firstShift.ShiftDate:yyyy-MM-dd}.");
                }

                // Assign the shift
                var shiftDto = await _plannedShiftService.AssignedShiftToStaffAsync(plannedShiftId, staffId);
                if (shiftDto == null)
                    return CreateError(call.Id, $"❌ Failed to assign shift ID {plannedShiftId} to staff ID {staffId}.");

                // Return success response
                var response = new
                {
                    success = true,
                    message = $"✅ Assigned {shiftDto.ShiftTypeName} shift on 📅 {shiftDto.ShiftDate:yyyy-MM-dd} (Slot {shiftDto.SlotNumber}) in 🏥 {shiftDto.ShiftDeparmentName} department to 👨‍⚕️ {shiftDto.AssignedStaffFullName}.",
                    assignedShift = shiftDto
                };

                string json = JsonSerializer.Serialize(response);
                _logger.LogInformation("Shift assigned successfully: {Json}", json);

                return new ToolOutput(call.Id, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception occurred while assigning shift.");
                return CreateError(call.Id, "❌ An internal error occurred while assigning the shift.");
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
