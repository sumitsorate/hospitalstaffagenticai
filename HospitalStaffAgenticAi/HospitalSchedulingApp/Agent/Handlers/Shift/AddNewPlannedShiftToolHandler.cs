using Azure.AI.Agents.Persistent;
using global::HospitalSchedulingApp.Agent.Tools.Shift;
using global::HospitalSchedulingApp.Common.Enums;
using global::HospitalSchedulingApp.Dal.Entities;
using global::HospitalSchedulingApp.Services.Interfaces;
using HospitalSchedulingApp.Common.Exceptions;
using HospitalSchedulingApp.Common.Extensions;
using HospitalSchedulingApp.Common.Handlers;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.Shift
{
    /// <summary>
    /// Tool handler responsible for creating a new planned shift.
    /// Validates inputs, enforces business rules, and persists the shift
    /// using <see cref="IPlannedShiftService"/>.
    /// </summary>
    public class AddNewPlannedShiftToolHandler : BaseToolHandler
    {
        private readonly IPlannedShiftService _plannedShiftService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddNewPlannedShiftToolHandler"/> class.
        /// </summary>
        /// <param name="plannedShiftService">The service responsible for planned shift management.</param>
        /// <param name="logger">The logger instance for diagnostic messages.</param>
        public AddNewPlannedShiftToolHandler(
            IPlannedShiftService plannedShiftService,
            ILogger<AddNewPlannedShiftToolHandler> logger)
            : base(logger)
        {
            _plannedShiftService = plannedShiftService;
        }

        /// <summary>
        /// Gets the tool name for registering this handler.
        /// </summary>
        public override string ToolName => AddNewPlannedShiftTool.GetTool().Name;

        /// <summary>
        /// Handles the tool call request to create a new planned shift.
        /// </summary>
        /// <param name="call">The incoming tool call request metadata.</param>
        /// <param name="root">The JSON payload containing shift details.</param>
        /// <returns>
        /// A <see cref="ToolOutput"/> representing either success (with created shift details)
        /// or an error message if validation or persistence fails.
        /// </returns>
        public override async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            try
            {
                // 📅 Validate shiftDate
                DateTime? shiftDate = root.FetchDateTime("shiftDate");
                if (shiftDate is null)
                {
                    return CreateError(call.Id, "❌ `shiftDate` is required and must be in yyyy-MM-dd format.");
                }

                // 🔄 Validate shiftTypeId (enum)
                int? shiftTypeIdInt = root.FetchInt("shiftTypeId");
                if (shiftTypeIdInt is null || !Enum.IsDefined(typeof(ShiftTypes), shiftTypeIdInt.Value))
                {
                    return CreateError(call.Id, "❌ `shiftTypeId` is required and must be a valid Shift Type.");
                }
                var shiftTypeId = (ShiftTypes)shiftTypeIdInt.Value;

                // 🏥 Validate departmentId
                int? departmentId = root.FetchInt("departmentId");
                if (departmentId is null || departmentId <= 0)
                {
                    return CreateError(call.Id, "❌ `departmentId` is required and must be a valid integer.");
                }

                // 🔢 Validate slotNumber
                int? slotNumber = root.FetchInt("slotNumber");
                if (slotNumber is null || slotNumber <= 0)
                {
                    return CreateError(call.Id, "❌ `slotNumber` is required and must be a valid integer.");
                }

                // 📝 Construct planned shift
                var newShift = new PlannedShift
                {
                    ShiftDate = shiftDate.Value,
                    ShiftTypeId = shiftTypeId,
                    DepartmentId = departmentId.Value,
                    SlotNumber = slotNumber.Value,
                    ShiftStatusId = ShiftStatuses.Vacant,
                    AssignedStaffId = null
                };

                try
                {
                    // ➕ Persist the new shift
                    var result = await _plannedShiftService.AddNewPlannedShiftAsync(newShift);

                    if (result == null)
                    {
                        return CreateError(call.Id, "❌ Failed to add the new planned shift.");
                    }

                    return CreateSuccess(
                        call.Id,
                        $"✅ New vacant {result.ShiftTypeName} shift added for 📅 {result.ShiftDate:yyyy-MM-dd} " +
                        $"(Slot {result.SlotNumber}) in 🏥 {result.ShiftDeparmentName}.",
                        result);
                }
                catch (BusinessRuleException ex)
                {
                    // Known business rule violation
                    return CreateError(call.Id, ex.Message);
                }
                catch (Exception)
                {
                    // Unexpected failure
                    return CreateError(call.Id, "❌ Failed to add the new planned shift.");
                }
            }
            catch (Exception)
            {
                // Fallback handler for unexpected runtime errors
                return CreateError(call.Id, "❌ An internal error occurred while adding the shift.");
            }
        }
    }
}
