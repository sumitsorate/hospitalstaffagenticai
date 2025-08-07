using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.Tools.Shift;
using HospitalSchedulingApp.Common.Enums;
using HospitalSchedulingApp.Dtos.LeaveRequest.Request;
using HospitalSchedulingApp.Dtos.Shift.Requests;
using HospitalSchedulingApp.Services.AuthServices.Interfaces;
using HospitalSchedulingApp.Services.Interfaces;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.Shift
{    

    namespace HospitalSchedulingApp.Agent.Handlers
    {
        public class UnassignShiftFromStaffToolHandler : IToolHandler
        {
            private readonly IPlannedShiftService _plannedShiftService;
            private readonly ILogger<UnassignShiftFromStaffToolHandler> _logger;
            private readonly IUserContextService _userContextService;

            public UnassignShiftFromStaffToolHandler(
                IPlannedShiftService plannedShiftService,
                ILogger<UnassignShiftFromStaffToolHandler> logger,
                IUserContextService userContextService)
            {
                _plannedShiftService = plannedShiftService;
                _logger = logger;
                _userContextService = userContextService;
            }

            public string ToolName => UnassignedShiftFromStaffTool.GetTool().Name;

            public async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
            {
                var isScheduler = _userContextService.IsScheduler();
                if (!isScheduler)
                {
                    return CreateError(call.Id, "🚫 You’re not authorized to unassign shifts. Only schedulers can perform this action.");
                }

                try
                {
                    if (!root.TryGetProperty("plannedShiftId", out var shiftIdProp) || !shiftIdProp.TryGetInt32(out var plannedShiftId))
                        return CreateError(call.Id, "❌ `plannedShiftId` is required and must be a valid integer.");

                    // Unassign the shift
                    var shiftDto = await _plannedShiftService.UnassignedShiftFromStaffAsync(plannedShiftId);
                    if (shiftDto == null)
                        return CreateError(call.Id, $"❌ Could not find or unassign shift with ID {plannedShiftId}.");

                    var response = new
                    {
                        success = true,
                        message = $"♻️ Successfully unassigned {shiftDto.ShiftTypeName} shift on 📅 {shiftDto.ShiftDate:yyyy-MM-dd} (Slot {shiftDto.SlotNumber}) in 🏥 {shiftDto.ShiftDeparmentName}.",
                        unassignedShift = shiftDto
                    };

                    string json = JsonSerializer.Serialize(response);
                    _logger.LogInformation("Shift unassigned successfully: {Json}", json);
                    return new ToolOutput(call.Id, json);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Exception occurred while unassigning shift.");
                    return CreateError(call.Id, "❌ An internal error occurred while unassigning the shift.");
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
}
