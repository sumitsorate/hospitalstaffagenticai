using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.Tools.LeaveRequest;
using HospitalSchedulingApp.Common.Enums;
using HospitalSchedulingApp.Dtos.LeaveRequest.Request;
using HospitalSchedulingApp.Services.Interfaces;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.LeaveRequest
{
    public class FetchLeaveRequestToolHandler : IToolHandler
    {
        private readonly ILeaveRequestService _leaveRequestService;
        private readonly ILogger<FetchLeaveRequestToolHandler> _logger;

        public FetchLeaveRequestToolHandler(
            ILeaveRequestService leaveRequestService,
            ILogger<FetchLeaveRequestToolHandler> logger)
        {
            _leaveRequestService = leaveRequestService;
            _logger = logger;
        }

        public string ToolName => FetchLeaveRequestTool.GetTool().Name;

        public async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            try
            {
                // Parse optional fields
                int? leaveRequestId = root.TryGetProperty("leaveRequestId", out var leaveRequestIdProp) && leaveRequestIdProp.TryGetInt32(out var lId) ? lId : null;

                // Parse optional fields
                int? staffId = root.TryGetProperty("staffId", out var staffIdProp) && staffIdProp.TryGetInt32(out var sid) ? sid : null;

                LeaveRequestStatuses? leaveStatusId = null;
                if (root.TryGetProperty("leaveStatusId", out var statusProp) && statusProp.TryGetInt32(out var statusVal) &&
                    Enum.IsDefined(typeof(LeaveRequestStatuses), statusVal))
                {
                    leaveStatusId = (LeaveRequestStatuses)statusVal;
                }

                DateTime? startDate = root.TryGetProperty("startDate", out var startProp) &&
                                      DateTime.TryParse(startProp.GetString(), out var start)
                                      ? start : null;

                DateTime? endDate = root.TryGetProperty("endDate", out var endProp) &&
                                    DateTime.TryParse(endProp.GetString(), out var end)
                                    ? end : null;

                LeaveType? leaveTypeId = null;
                if (root.TryGetProperty("leaveTypeId", out var typeProp) && typeProp.TryGetInt32(out var typeVal) &&
                    Enum.IsDefined(typeof(LeaveType), typeVal))
                {
                    leaveTypeId = (LeaveType)typeVal;
                }

                // Construct filter
                var filter = new LeaveRequestFilter
                {
                    LeaveRequestId = leaveRequestId,
                    StaffId = staffId,
                    LeaveStatusId = leaveStatusId,
                    StartDate = startDate,
                    EndDate = endDate,
                    LeaveTypeId = leaveTypeId
                };

                // Fetch results
                var leaveRequests = await _leaveRequestService.FetchLeaveRequestsAsync(filter);

                var response = new
                {
                    success = true,
                    message = "Leave requests fetched successfully.",
                    data = leaveRequests
                };

                var json = JsonSerializer.Serialize(response);
                _logger.LogInformation("Leave requests fetched: {Json}", json);

                return new ToolOutput(call.Id, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FetchLeaveRequestToolHandler");
                return CreateError(call.Id, "An internal error occurred while fetching leave requests.");
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
