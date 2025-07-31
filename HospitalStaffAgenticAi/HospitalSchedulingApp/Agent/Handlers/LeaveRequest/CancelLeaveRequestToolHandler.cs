using Azure;
using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.Tools.LeaveRequest;
using HospitalSchedulingApp.Common.Enums;
using HospitalSchedulingApp.Dal.Entities;
using HospitalSchedulingApp.Services.Helpers;
using HospitalSchedulingApp.Services.Interfaces;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.LeaveRequest
{
    /// <summary>
    /// Handler for cancelling a leave request using the CancelLeaveRequestTool.
    /// </summary>
    public class CancelLeaveRequestToolHandler : IToolHandler
    {
        private readonly ILeaveRequestService _leaveRequestService;
        private readonly ILeaveTypeService _leaveTypeService;
        private readonly ILogger<CancelLeaveRequestToolHandler> _logger;

        public CancelLeaveRequestToolHandler(
            ILeaveRequestService leaveRequestService,
            ILeaveTypeService leaveTypeService,
            ILogger<CancelLeaveRequestToolHandler> logger)
        {
            _leaveRequestService = leaveRequestService;
            _leaveTypeService = leaveTypeService;
            _logger = logger;
        }

        public string ToolName => CancelLeaveRequestTool.GetTool().Name;

        public async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            try
            {
                // 👤 staffId
                if (!root.TryGetProperty("staffId", out var staffIdProp) ||
                    !staffIdProp.TryGetInt32(out var staffId) || staffId <= 0)
                {
                    return CreateError(call.Id, "Missing or invalid staffId.");
                }

                // 📅 leaveStart
                if (!root.TryGetProperty("leaveStart", out var startProp) ||
                    !DateTime.TryParse(startProp.GetString(), out var leaveStart))
                {
                    return CreateError(call.Id, "Invalid or missing leaveStart (format: YYYY-MM-DD).");
                }

                // 📅 leaveEnd
                if (!root.TryGetProperty("leaveEnd", out var endProp) ||
                    !DateTime.TryParse(endProp.GetString(), out var leaveEnd))
                {
                    return CreateError(call.Id, "Invalid or missing leaveEnd (format: YYYY-MM-DD).");
                }

                if (leaveEnd < leaveStart)
                {
                    return CreateError(call.Id, "leaveEnd must be on or after leaveStart.");
                }

                // ✅ Check if leave exists
                var existing = await _leaveRequestService.FetchLeaveRequestInfoAsync(staffId, leaveStart, leaveEnd);
                if (existing == null)
                {
                    return CreateError(call.Id, "No existing leave request found for the given period.");
                }

                // ❌ Cancel the leave
                var cancelled = await _leaveRequestService.CancelLeaveRequestAsync(existing);

                if (cancelled != null)
                {
                    var response = new
                    {
                        success = true,
                        message = "Leave request cancelled successfully.",
                        data = new
                        {
                            cancelled.StaffId,
                            cancelled.LeaveStart,
                            cancelled.LeaveEnd
                        }
                    };

                    var json = JsonSerializer.Serialize(response);
                    _logger.LogInformation("Leave request cancelled: {Json}", json);
                    return new ToolOutput(call.Id, json);

                }
                return CreateError(call.Id, "Unable to cancel the leave request.");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CancelLeaveRequestToolHandler");
                return CreateError(call.Id, "An internal error occurred while cancelling the leave request.");
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
