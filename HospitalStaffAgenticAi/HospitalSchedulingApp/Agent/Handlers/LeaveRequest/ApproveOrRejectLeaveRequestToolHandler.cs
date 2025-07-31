using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.Tools.LeaveRequest;
using HospitalSchedulingApp.Common.Enums;
using HospitalSchedulingApp.Dtos.LeaveRequest.Request;
using HospitalSchedulingApp.Dtos.LeaveRequest.Response;
using HospitalSchedulingApp.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.LeaveRequest
{
    public class ApproveOrRejectLeaveRequestToolHandler : IToolHandler
    {
        private readonly ILeaveRequestService _leaveRequestService;
        private readonly ILogger<ApproveOrRejectLeaveRequestToolHandler> _logger;

        public ApproveOrRejectLeaveRequestToolHandler(
            ILeaveRequestService leaveRequestService,
            ILogger<ApproveOrRejectLeaveRequestToolHandler> logger)
        {
            _leaveRequestService = leaveRequestService;
            _logger = logger;
        }

        public string ToolName => ApproveOrRejectLeaveRequestTool.GetTool().Name;

        public async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            try
            {
                int? leaveRequestId = root.TryGetProperty("leaveRequestId", out var idProp) && idProp.TryGetInt32(out var lid) ? lid : null;
                int? staffId = root.TryGetProperty("staffId", out var staffProp) && staffProp.TryGetInt32(out var sid) ? sid : null;
                int? leaveTypeId = root.TryGetProperty("leaveTypeId", out var typeProp) && typeProp.TryGetInt32(out var ltid) ? ltid : null;

                DateTime? startDate = root.TryGetProperty("startDate", out var startProp) &&
                                      DateTime.TryParse(startProp.GetString(), out var start)
                                      ? start : null;

                DateTime? endDate = root.TryGetProperty("endDate", out var endProp) &&
                                    DateTime.TryParse(endProp.GetString(), out var end)
                                    ? end : null;

                LeaveRequestStatuses? newStatus = null;
                if (root.TryGetProperty("newStatus", out var statusProp))
                {
                    var statusString = statusProp.GetString();
                    if (Enum.TryParse<LeaveRequestStatuses>(statusString, true, out var statusEnum) &&
                        (statusEnum == LeaveRequestStatuses.Approved || statusEnum == LeaveRequestStatuses.Rejected))
                    {
                        newStatus = statusEnum;
                    }
                }

                if (!newStatus.HasValue)
                {
                    return CreateError(call.Id, "Invalid or missing newStatus. It must be either 'Approved' or 'Rejected'.");
                }

                // Build filter
                var leaveRequestFilter = new LeaveRequestFilter
                {
                    LeaveRequestId = leaveRequestId,
                    StaffId = staffId,
                    StartDate = startDate,
                    EndDate = endDate,
                    LeaveStatusId = LeaveRequestStatuses.Pending
                };

                var matchingRequests = await _leaveRequestService.FetchLeaveRequestsAsync(leaveRequestFilter);

                if (!matchingRequests.Any())
                {
                    return CreateError(call.Id, "No pending leave requests matched the given criteria.");
                }

                // Use the first matched leave request
                var requestToUpdate = matchingRequests.First();
                LeaveRequestDetailsDto? updatedRequest = await _leaveRequestService.UpdateStatusAsync(requestToUpdate.LeaveRequestId, newStatus.Value);

                if (updatedRequest == null)
                {
                    return CreateError(call.Id, "Failed to update leave request. It may not exist or has already been processed.");
                }

                var response = new
                {
                    success = true,
                    message = $"Leave request {newStatus.Value.ToString().ToLower()} successfully.",
                    leaveRequest = updatedRequest
                };

                string json = JsonSerializer.Serialize(response);
                _logger.LogInformation("Leave request update result: {Json}", json);

                return new ToolOutput(call.Id, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ApproveOrRejectLeaveRequestToolHandler");
                return CreateError(call.Id, "An internal error occurred while processing the leave request.");
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
