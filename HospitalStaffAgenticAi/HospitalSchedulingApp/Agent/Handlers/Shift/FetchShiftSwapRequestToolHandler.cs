using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.Handlers.LeaveRequest;
using HospitalSchedulingApp.Agent.Tools.Shift;
using HospitalSchedulingApp.Common.Enums;
using HospitalSchedulingApp.Dtos.Shift.Requests;
using HospitalSchedulingApp.Services;
using HospitalSchedulingApp.Services.AuthServices.Interfaces;
using HospitalSchedulingApp.Services.Interfaces;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.Shift
{
    public class FetchShiftSwapRequestToolHandler : IToolHandler
    {
        private readonly IShiftSwapService _shiftSwapService;
        private readonly ILogger<FetchLeaveRequestToolHandler> _logger;
        private readonly IUserContextService _userContextService;
        public FetchShiftSwapRequestToolHandler(
            IShiftSwapService shiftSwapService,
            ILogger<FetchLeaveRequestToolHandler> logger,
            IUserContextService userContextService
            )
        {
            _logger = logger;
            _shiftSwapService = shiftSwapService;
            _userContextService = userContextService;
        }

        public string ToolName => FetchShiftSwapRequestTool.GetTool().Name;

        public async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            try
            {
                var filter = new ShiftSwapDto
                {
                    StatusId = root.TryGetProperty("statusId", out var statusProp) && statusProp.TryGetInt32(out var statusId) ? (ShiftSwapStatuses?)statusId : null,
                    requesterStaffId = root.TryGetProperty("requesterStaffId", out var requesterStaffProp) && requesterStaffProp.TryGetInt32(out var requesterStaffId) ? requesterStaffId : null,
                    targetStaffId = root.TryGetProperty("targetStaffId", out var targetStaffProp) && targetStaffProp.TryGetInt32(out var targetStaffId) ? targetStaffId : null,
                    requesterShiftTypeId = root.TryGetProperty("requesterShiftTypeId", out var requesterShiftTypeProp) && requesterShiftTypeProp.TryGetInt32(out var requesterShiftTypeId) ? requesterShiftTypeId : null,
                    targetShiftTypeId = root.TryGetProperty("targetShiftTypeId", out var targetShiftTypeProp) && targetShiftTypeProp.TryGetInt32(out var targetShiftTypeId) ? targetShiftTypeId : null,
                    fromDate = root.TryGetProperty("fromDate", out var fromDateProp) && DateTime.TryParse(fromDateProp.GetString(), out var fromDate) ? fromDate : null,
                    toDate = root.TryGetProperty("toDate", out var toDateProp) && DateTime.TryParse(toDateProp.GetString(), out var toDate) ? toDate : null
                };

                // Fetch results from service
                var swapRequests = await _shiftSwapService.FetchShiftSwapRequestsAsync(filter);

                var response = new
                {
                    success = true,
                    message = "😊 Shift swap request requests fetched successfully!",
                    data = swapRequests
                };

                var json = JsonSerializer.Serialize(response);
                _logger.LogInformation("🙁 Shift swap request fetched: {Json}", json);

                return new ToolOutput(call.Id, json);
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError(ex, "Error in FetchShiftSwapRequestToolHandler");
                return CreateError(call.Id, "❌ An internal error occurred while fetching shift swap requests.");
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
