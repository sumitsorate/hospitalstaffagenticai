using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.Tools.LeaveRequest;
using HospitalSchedulingApp.Common.Enums;
using HospitalSchedulingApp.Dtos.LeaveRequest.Request;
using HospitalSchedulingApp.Services.AuthServices.Interfaces;
using HospitalSchedulingApp.Services.Interfaces;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.LeaveRequest
{
    public class FetchLeaveRequestToolHandler : IToolHandler
    {
        private readonly ILeaveRequestService _leaveRequestService;
        private readonly ILogger<FetchLeaveRequestToolHandler> _logger;
        private readonly IUserContextService _userContextService;

        public FetchLeaveRequestToolHandler(
            ILeaveRequestService leaveRequestService,
            ILogger<FetchLeaveRequestToolHandler> logger,
            IUserContextService userContextService)
        {
            _leaveRequestService = leaveRequestService;
            _logger = logger;
            _userContextService = userContextService;
        }

        public string ToolName => FetchLeaveRequestTool.GetTool().Name;

        public async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            try
            {
                // Parse optional fields
                int? leaveRequestId = root.TryGetProperty("leaveRequestId", out var prop) && prop.TryGetInt32(out var val) ? val : null;
                int? staffId = root.TryGetProperty("staffId", out var prop2) && prop2.TryGetInt32(out var val2) ? val2 : null;

                LeaveRequestStatuses? leaveStatusId = null;
                if (root.TryGetProperty("leaveStatusId", out var prop3) && prop3.TryGetInt32(out var val3) &&
                    Enum.IsDefined(typeof(LeaveRequestStatuses), val3))
                {
                    leaveStatusId = (LeaveRequestStatuses)val3;
                }

                DateTime? startDate = root.TryGetProperty("startDate", out var startProp) &&
                                      DateTime.TryParse(startProp.GetString(), out var start)
                                      ? start : null;

                DateTime? endDate = root.TryGetProperty("endDate", out var endProp) &&
                                    DateTime.TryParse(endProp.GetString(), out var end)
                                    ? end : null;

                LeaveType? leaveTypeId = null;
                if (root.TryGetProperty("leaveTypeId", out var typeProp) &&
                    typeProp.TryGetInt32(out var typeVal) &&
                    Enum.IsDefined(typeof(LeaveType), typeVal))
                {
                    leaveTypeId = (LeaveType)typeVal;
                }

           

                // Build filter object
                var filter = new LeaveRequestFilter
                {
                    LeaveRequestId = leaveRequestId,
                    StaffId = staffId,
                    LeaveStatusId = leaveStatusId,
                    StartDate = startDate,
                    EndDate = endDate,
                    LeaveTypeId = leaveTypeId
                };

                // 🔒 Permission check
                var isEmployee = _userContextService.IsEmployee();
                var loggedInUserStaffId = _userContextService.GetStaffId();

                if (isEmployee)
                {
                    if (filter.StaffId.HasValue && filter.StaffId != loggedInUserStaffId)
                    {
                        return CreateError(call.Id, "🚫 You're only allowed to view your own leave requests.");
                    }

                    // Enforce own ID even if null or tampered
                    filter.StaffId = loggedInUserStaffId;
                }


                // Fetch results from service
                var leaveRequests = await _leaveRequestService.FetchLeaveRequestsAsync(filter);

                var response = new
                {
                    success = true,
                    message = "📋 Leave requests fetched successfully!",
                    data = leaveRequests
                };

                var json = JsonSerializer.Serialize(response);
                _logger.LogInformation("Leave requests fetched: {Json}", json);

                return new ToolOutput(call.Id, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FetchLeaveRequestToolHandler");
                return CreateError(call.Id, "❌ An internal error occurred while fetching leave requests.");
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
