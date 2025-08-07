
using Azure.AI.Agents;
using Azure.AI.Agents.Persistent;
using global::HospitalSchedulingApp.Agent.Tools.Shift;
using global::HospitalSchedulingApp.Common.Enums;
using global::HospitalSchedulingApp.Dal.Entities;
using global::HospitalSchedulingApp.Services.Interfaces;
 
using System.Text.Json;


namespace HospitalSchedulingApp.Agent.Handlers.Shift
{
    public class AddNewPlannedShiftToolHandler : IToolHandler
    {
        private readonly IPlannedShiftService _plannedShiftService;
        private readonly ILogger<AddNewPlannedShiftToolHandler> _logger;

        public AddNewPlannedShiftToolHandler(
            IPlannedShiftService plannedShiftService,
            ILogger<AddNewPlannedShiftToolHandler> logger)
        {
            _plannedShiftService = plannedShiftService;
            _logger = logger;
        }

        public string ToolName => AddNewPlannedShiftTool.GetTool().Name;

        public async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            try
            {
                // Validate required parameters
                if (!root.TryGetProperty("shiftDate", out var dateProp) || !DateTime.TryParse(dateProp.GetString(), out var shiftDate))
                    return CreateError(call.Id, "❌ `shiftDate` is required and must be in yyyy-MM-dd format.");

                if (!root.TryGetProperty("shiftTypeId", out var typeProp) || !typeProp.TryGetInt32(out var shiftTypeId))
                    return CreateError(call.Id, "❌ `shiftTypeId` is required and must be a valid integer.");

                if (!root.TryGetProperty("departmentId", out var deptProp) || !deptProp.TryGetInt32(out var departmentId))
                    return CreateError(call.Id, "❌ `departmentId` is required and must be a valid integer.");

                if (!root.TryGetProperty("slotNumber", out var slotProp) || !slotProp.TryGetInt32(out var slotNumber))
                    return CreateError(call.Id, "❌ `slotNumber` is required and must be a valid integer.");

                // Construct planned shift
                var newShift = new PlannedShift
                {
                    ShiftDate = shiftDate,
                    ShiftTypeId = (ShiftTypes)shiftTypeId,
                    DepartmentId = departmentId,
                    SlotNumber = slotNumber,
                    ShiftStatusId = ShiftStatuses.Vacant,
                    AssignedStaffId = null
                };

                // Add shift
                var result = await _plannedShiftService.AddNewPlannedShiftAsync(newShift);
                if (result == null)
                    return CreateError(call.Id, "❌ Failed to add the new planned shift.");

                // Success response
                var response = new
                {
                    success = true,
                    message = $"✅ New vacant {result.ShiftTypeName} shift added for 📅 {result.ShiftDate:yyyy-MM-dd} (Slot {result.SlotNumber}) in 🏥 {result.ShiftDeparmentName}.",
                    shift = result
                };

                string json = JsonSerializer.Serialize(response);
                _logger.LogInformation("New planned shift added: {Json}", json);

                return new ToolOutput(call.Id, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception while adding new planned shift.");
                return CreateError(call.Id, "❌ An internal error occurred while adding the shift.");
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
