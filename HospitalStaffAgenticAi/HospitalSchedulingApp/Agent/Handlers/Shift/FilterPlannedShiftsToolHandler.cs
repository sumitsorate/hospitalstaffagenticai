using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.Tools.Shift;
using HospitalSchedulingApp.Common.Extensions;
using HospitalSchedulingApp.Common.Handlers;
using HospitalSchedulingApp.Dtos.Shift.Requests;
using HospitalSchedulingApp.Services.AuthServices.Interfaces;
using HospitalSchedulingApp.Services.Interfaces;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.Shift
{
    public class FilterPlannedShiftsToolHandler : BaseToolHandler
    {
        private readonly IPlannedShiftService _plannedShiftService;

        public FilterPlannedShiftsToolHandler(
            IPlannedShiftService plannedShiftService,
            ILogger<FilterPlannedShiftsToolHandler> logger,
            IUserContextService userContextService)
            : base(logger)
        {
            _plannedShiftService = plannedShiftService;
        }

        public override string ToolName => FilterShiftScheduleTool.GetTool().Name;

        public override async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            try
            {
                // Build filter from request
                var filter = new ShiftFilterDto
                {
                    StaffId = root.FetchInt("staffId"),
                    DepartmentId = root.FetchInt("departmentId"),
                    ShiftTypeId = root.FetchInt("shiftTypeId"),
                    ShiftStatusId = root.FetchInt("shiftStatusId"),
                    FromDate = root.FetchDateTime("fromDate"),
                    ToDate = root.FetchDateTime("toDate"),
                    SlotNumber = root.FetchInt("slotNumber")
                };

                var results = await _plannedShiftService.FetchFilteredPlannedShiftsAsync(filter);
                return CreateSuccess(callId: call.Id, "✅ Shift Fetch Successfully", results);
            }
            catch (Exception ex)
            {
                return CreateError(call.Id, "❌ An unexpected error occurred while filtering planned shifts.");
            }
        }
    }
}
