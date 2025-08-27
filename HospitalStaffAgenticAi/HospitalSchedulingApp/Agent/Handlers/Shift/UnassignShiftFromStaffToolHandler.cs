using Azure.AI.Agents.Persistent;
using Azure.Core;
using HospitalSchedulingApp.Agent.Tools.Shift;
using HospitalSchedulingApp.Common.Exceptions;
using HospitalSchedulingApp.Common.Extensions;
using HospitalSchedulingApp.Common.Handlers;
using HospitalSchedulingApp.Services.AuthServices.Interfaces;
using HospitalSchedulingApp.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.Shift
{
    /// <summary>
    /// ♻️ Handler for unassigning a shift from a staff member.
    /// </summary>
    public class UnassignShiftFromStaffToolHandler : BaseToolHandler
    {
        private readonly IPlannedShiftService _plannedShiftService;
        private readonly IUserContextService _userContextService;

        public UnassignShiftFromStaffToolHandler(
            IPlannedShiftService plannedShiftService,
            ILogger<UnassignShiftFromStaffToolHandler> logger,
            IUserContextService userContextService)
            : base(logger)
        {
            _plannedShiftService = plannedShiftService;
            _userContextService = userContextService;
        }

        public override string ToolName => UnassignedShiftFromStaffTool.GetTool().Name;

        public override async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            if (!_userContextService.IsScheduler())
            {
                return CreateError(call.Id,
                    "🚫 You’re not authorized to unassign shifts. Only schedulers can perform this action.");
            }

            // 🆔 Validate plannedShiftId
            int? plannedShiftId = root.FetchInt("plannedShiftId");
            if (plannedShiftId is null || plannedShiftId <= 0)
            {
                return CreateError(call.Id,
                    "❌ `plannedShiftId` is required and must be a valid integer.");
            }

            try
            {
                // ♻️ Unassign the shift
                var shiftDto = await _plannedShiftService.UnassignedShiftFromStaffAsync(plannedShiftId.Value);
                if (shiftDto == null)
                {
                    return CreateError(call.Id,
                        $"❌ Could not find or unassign shift with ID {plannedShiftId}.");
                }

                return CreateSuccess(call.Id,
                    $"♻️ Successfully unassigned {shiftDto.ShiftTypeName} shift on 📅 {shiftDto.ShiftDate:yyyy-MM-dd} (Slot {shiftDto.SlotNumber}) in 🏥 {shiftDto.ShiftDeparmentName}.",
                    shiftDto);
            }
            catch (BusinessRuleException ex)
            {
                return CreateError(call.Id, $"❌ {ex.Message}");
            }
            catch (Exception ex)
            { 
                return CreateError(call.Id,
                    "❌ An internal error occurred while unassigning the shift.");
            }
        }
    }
}
