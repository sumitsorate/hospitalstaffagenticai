using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.Handlers.LeaveRequest;
using HospitalSchedulingApp.Agent.Tools.Shift;
using HospitalSchedulingApp.Common.Enums;
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
                // Fetch results from service
                var leaveRequests = await _shiftSwapService.FetchShiftSwapRequestsAsync(ShiftSwapStatuses.Pending);

                var response = new
                {
                    success = true,
                    message = "🔄 Shift swap request requests fetched successfully!",
                    data = leaveRequests
                };

                var json = JsonSerializer.Serialize(response);
                _logger.LogInformation("Shift swap request fetched: {Json}", json);

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
