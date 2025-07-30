﻿using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.Tools.LeaveRequest;
using HospitalSchedulingApp.Agent.Tools.Shift;
using HospitalSchedulingApp.Common.Enums;
using HospitalSchedulingApp.Dal.Entities;
using HospitalSchedulingApp.Services.Interfaces;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.LeaveRequest
{
    /// <summary>
    /// Handler for submitting a leave request using the SubmitLeaveRequestTool.
    /// </summary>
    public class SubmitLeaveRequestToolHandler : IToolHandler
    {
        private readonly ILeaveRequestService _leaveRequestService;
        private readonly ILogger<SubmitLeaveRequestToolHandler> _logger;

        public SubmitLeaveRequestToolHandler(
            ILeaveRequestService leaveRequestService,
            ILogger<SubmitLeaveRequestToolHandler> logger)
        {
            _leaveRequestService = leaveRequestService;
            _logger = logger;
        }

        public string ToolName => SubmitLeaveRequestTool.GetTool().Name;
     

        public async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            try
            {
                // 👤 Get staffId
                if (!root.TryGetProperty("staffId", out var staffIdProp) ||
                    !staffIdProp.TryGetInt32(out var staffId) || staffId <= 0)
                {
                    return CreateError(call.Id, "Missing or invalid staffId.");
                }

                // 📅 Parse leaveStart
                if (!root.TryGetProperty("leaveStart", out var startProp) ||
                    !DateTime.TryParse(startProp.GetString(), out var leaveStart))
                {
                    return CreateError(call.Id, "Invalid or missing leaveStart (format: YYYY-MM-DD).");
                }

                // 📅 Parse leaveEnd
                if (!root.TryGetProperty("leaveEnd", out var endProp) ||
                    !DateTime.TryParse(endProp.GetString(), out var leaveEnd))
                {
                    return CreateError(call.Id, "Invalid or missing leaveEnd (format: YYYY-MM-DD).");
                }

                if (leaveEnd < leaveStart)
                {
                    return CreateError(call.Id, "leaveEnd must be on or after leaveStart.");
                }

                // 🏷️ Optional leaveType
                // 🏷️ Required leaveTypeId
                if (!root.TryGetProperty("leaveTypeId", out var leaveTypeProp) ||
                    !leaveTypeProp.TryGetInt32(out var leaveTypeIdInt) ||
                    !Enum.IsDefined(typeof(LeaveType), leaveTypeIdInt))
                {
                    return CreateError(call.Id, "Invalid or missing leaveTypeId. Must be one of the supported leave types.");
                }

                var leaveTypeId = (LeaveType)leaveTypeIdInt;



                // 📝 Construct leave request
                var leaveRequest = new LeaveRequests
                {
                    StaffId = staffId,
                    LeaveStart = leaveStart,
                    LeaveEnd = leaveEnd,
                    LeaveTypeId = leaveTypeId,
                    LeaveStatusId = Common.Enums.LeaveRequestStatuses.Pending                    
                };

                var isOverlap = await _leaveRequestService.CheckIfLeaveAlreadyExists(leaveRequest);
                if (isOverlap)
                {
                    return CreateError(call.Id, "Leave already exists for the employee for same date");
                }

                var savedRequest = await _leaveRequestService.SubmitLeaveRequestAsync(leaveRequest);

                var response = new
                {
                    success = true,
                    message = "Leave request submitted successfully.",
                    data = new
                    {
                        savedRequest.Id,
                        savedRequest.StaffId,
                        savedRequest.LeaveStart,
                        savedRequest.LeaveEnd,
                        Common.Enums.LeaveRequestStatuses.Pending                         
                    }
                };

                var json = JsonSerializer.Serialize(response);
                _logger.LogInformation("Leave request submitted: {Json}", json);
                return new ToolOutput(call.Id, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SubmitLeaveRequestToolHandler");
                return CreateError(call.Id, "An internal error occurred while submitting the leave request.");
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
