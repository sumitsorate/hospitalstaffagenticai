using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.Tools.Shift;
using HospitalSchedulingApp.Dtos.Shift.Requests;
using HospitalSchedulingApp.Services.AuthServices.Interfaces;
using HospitalSchedulingApp.Services.Interfaces;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.Shift
{
    public class FilterPlannedShiftsToolHandler : IToolHandler
    {
        private readonly IPlannedShiftService _plannedShiftService;
        private readonly ILogger<FilterPlannedShiftsToolHandler> _logger;
        private readonly IUserContextService _userContextService;

        public FilterPlannedShiftsToolHandler(
            IPlannedShiftService plannedShiftService,
            ILogger<FilterPlannedShiftsToolHandler> logger,
            IUserContextService userContextService)
        {
            _plannedShiftService = plannedShiftService;
            _logger = logger;
            _userContextService = userContextService;
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

                var isEmployee = _userContextService.IsEmployee();
                var loggedInUserStaffID = _userContextService.GetStaffId();

                if (isEmployee)
                {
                    // 🔐 Force employee to see only their own shifts
                    if (filter.StaffId.HasValue && filter.StaffId != loggedInUserStaffID)
                    {
                        return CreateError(call.Id, "🚫 You're only allowed to view your own shift schedule.");
                    }

                    // Even if not specified, restrict to logged-in user
                    filter.StaffId = loggedInUserStaffID;
                }

                _logger.LogInformation("Filtering shifts with: {@Filter}", filter);

                var results = await _plannedShiftService.FetchFilteredPlannedShiftsAsync(filter);

                // Success response
                var response = new
                {
                    success = true,
                    message = $"✅ Shifts Fetched Successfully",
                    shift = results
                };

                string json = JsonSerializer.Serialize(response);
                _logger.LogInformation("Shifts Fetched Successfully: {Json}", json);

                //var json = JsonSerializer.Serialize(results, new JsonSerializerOptions
                //{
                //    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                //});

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

