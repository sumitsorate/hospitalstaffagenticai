using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.Tools.Shift;
using HospitalSchedulingApp.Dtos.Shift.Requests;
using HospitalSchedulingApp.Services.Interfaces;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.Shift
{
    public class FilterPlannedShiftsToolHandler : IToolHandler
    {
        private readonly IPlannedShiftService _plannedShiftService;
        private readonly ILogger<FilterPlannedShiftsToolHandler> _logger;

        public FilterPlannedShiftsToolHandler(
            IPlannedShiftService plannedShiftService,
            ILogger<FilterPlannedShiftsToolHandler> logger)
        {
            _plannedShiftService = plannedShiftService;
            _logger = logger;
        }

        public string ToolName => FilterShiftScheduleTool.GetTool().Name;

        public async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            try
            {
                var filter = new ShiftFilterDto
                {
                    StaffId = root.TryGetProperty("staffId", out var staffIdProp) && staffIdProp.TryGetInt32(out var staffId) ? staffId : null,
                    DepartmentId = root.TryGetProperty("departmentId", out var deptIdProp) && deptIdProp.TryGetInt32(out var deptId) ? deptId : null,
                    ShiftTypeId = root.TryGetProperty("shiftTypeId", out var typeProp) && typeProp.TryGetInt32(out var shiftTypeId) ? shiftTypeId : null,
                    ShiftStatusId = root.TryGetProperty("shiftStatusId", out var statusProp) && statusProp.TryGetInt32(out var shiftStatusId) ? shiftStatusId : null,
                    FromDate = root.TryGetProperty("fromDate", out var fromProp) && DateTime.TryParse(fromProp.GetString(), out var fromDate) ? fromDate : null,
                    ToDate = root.TryGetProperty("toDate", out var toProp) && DateTime.TryParse(toProp.GetString(), out var toDate) ? toDate : null,
                    SlotNumber = root.TryGetProperty("slotNumber", out var slotNumberProp) && slotNumberProp.TryGetInt32(out var slotNumber) ? slotNumber : null,
                };

                _logger.LogInformation("Filtering shifts with: {@Filter}", filter);

                var results = await _plannedShiftService.FetchFilteredPlannedShiftsAsync(filter);

                var json = JsonSerializer.Serialize(results, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                return new ToolOutput(call.Id, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while filtering planned shifts.");
                return ErrorOutput(call.Id, "An unexpected error occurred while filtering planned shifts.");
            }
        }

        private ToolOutput ErrorOutput(string callId, string message)
        {
            var errorJson = JsonSerializer.Serialize(new
            {
                success = false,
                error = message
            });

            return new ToolOutput(callId, errorJson);
        }
    }
}

