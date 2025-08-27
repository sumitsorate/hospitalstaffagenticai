using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.Tools.Shift;
using HospitalSchedulingApp.Common.Enums;
using HospitalSchedulingApp.Common.Extensions;
using HospitalSchedulingApp.Common.Handlers;
using HospitalSchedulingApp.Dal.Entities;
using HospitalSchedulingApp.Dtos.LeaveRequest.Request;
using HospitalSchedulingApp.Dtos.Shift.Requests;
using HospitalSchedulingApp.Services.AuthServices.Interfaces;
using HospitalSchedulingApp.Services.Interfaces;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.Shift
{
    public class AssignShiftToStaffToolHandler : BaseToolHandler
    {
        private readonly IPlannedShiftService _plannedShiftService;
        private readonly ILeaveRequestService _leaveRequestService;
        private readonly IUserContextService _userContextService;

        public AssignShiftToStaffToolHandler(
            IPlannedShiftService plannedShiftService,
            ILogger<AssignShiftToStaffToolHandler> logger,
            ILeaveRequestService leaveRequestService,
            IUserContextService userContextService)
            : base(logger)
        {
            _plannedShiftService = plannedShiftService;
            _leaveRequestService = leaveRequestService;
            _userContextService = userContextService;
        }

        public override string ToolName => AssignShiftToStaffTool.GetTool().Name;

        public override async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            // 🔒 Scheduler-only check
            if (!_userContextService.IsScheduler())
            {
                return CreateError(call.Id,
                    "🚫 Oops! You're not authorized to perform this action. " +
                    "Let me know if you need help with something else.");
            }

            try
            {
                // 🆔 Validate plannedShiftId
                int? plannedShiftId = root.FetchInt("plannedShiftId");
                if (plannedShiftId is null || plannedShiftId <= 0)
                {
                    return CreateError(call.Id, "❌ `plannedShiftId` is required and must be a valid integer.");
                }

                // 👤 Validate staffId
                int? staffId = root.FetchInt("staffId");
                if (staffId is null || staffId <= 0)
                {
                    return CreateError(call.Id, "❌ `staffId` is required and must be a valid integer.");
                }

                

                // ✅ Assign shift
                var shiftDto = await _plannedShiftService.AssignedShiftToStaffAsync(plannedShiftId.Value, staffId.Value);
                if (shiftDto == null)
                {
                    return CreateError(call.Id, $"❌ Failed to assign shift ID {plannedShiftId} to staff ID {staffId}.");
                }

                return CreateSuccess(call.Id,
                    $"✅ Assigned {shiftDto.ShiftTypeName} shift on 📅 {shiftDto.ShiftDate:yyyy-MM-dd} " +
                    $"(Slot {shiftDto.SlotNumber}) in 🏥 {shiftDto.ShiftDeparmentName} department " +
                    $"to 👨‍⚕️ {shiftDto.AssignedStaffFullName}",
                    shiftDto);

            }
            catch (Exception ex)
            {
                return CreateError(call.Id, "❌ An internal error occurred while assigning the shift.");
            }
        }
    }
}
