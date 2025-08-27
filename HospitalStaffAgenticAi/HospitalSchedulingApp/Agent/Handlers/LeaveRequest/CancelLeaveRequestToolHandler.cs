using Azure.AI.Agents.Persistent;
using Castle.Core.Logging;
using HospitalSchedulingApp.Agent.Tools.LeaveRequest;
using HospitalSchedulingApp.Common.Exceptions;
using HospitalSchedulingApp.Common.Extensions;
using HospitalSchedulingApp.Common.Handlers;
using HospitalSchedulingApp.Services.AuthServices.Interfaces;
using HospitalSchedulingApp.Services.Helpers;
using HospitalSchedulingApp.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.LeaveRequest
{
    /// <summary>
    /// Handler for cancelling a leave request using the CancelLeaveRequestTool.
    /// </summary>
    public class CancelLeaveRequestToolHandler : BaseToolHandler
    {
        private readonly ILeaveRequestService _leaveRequestService;
        private readonly ILeaveTypeService _leaveTypeService;
        private readonly IUserContextService _userContextService;

        public CancelLeaveRequestToolHandler(
            ILeaveRequestService leaveRequestService,
            ILeaveTypeService leaveTypeService,
            ILogger<CancelLeaveRequestToolHandler> logger,
            IUserContextService userContextService)
            : base(logger)
        {
            _leaveRequestService = leaveRequestService;
            _leaveTypeService = leaveTypeService;
            _userContextService = userContextService;
        }

        public override string ToolName => CancelLeaveRequestTool.GetTool().Name;

        public override async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            try
            {
                // 👤 staffId
                int? staffId = root.FetchInt("staffId");
                if (staffId is null || staffId <= 0)
                {
                    return CreateError(call.Id, "Missing or invalid staffId.");
                }

                // 📅 leaveStart
                DateTime? leaveStart = root.FetchDateTime("leaveStart");
                if (leaveStart is null)
                {
                    return CreateError(call.Id, "Invalid or missing leaveStart (format: YYYY-MM-DD).");
                }

                // 📅 leaveEnd
                DateTime? leaveEnd = root.FetchDateTime("leaveEnd");
                if (leaveEnd is null)
                {
                    return CreateError(call.Id, "Invalid or missing leaveEnd (format: YYYY-MM-DD).");
                }

                if (leaveEnd < leaveStart)
                {
                    return CreateError(call.Id, "leaveEnd must be on or after leaveStart.");
                }
                 


                // ✅ Check if leave exists
                var existing = await _leaveRequestService.FetchLeaveRequestInfoAsync(
                    staffId.Value,
                    leaveStart.Value,
                    leaveEnd.Value);

                if (existing == null)
                {
                    return CreateError(call.Id, "No existing leave request found for the given period.");
                }


                // ❌ Cancel the leave
                var cancelled = await _leaveRequestService.CancelLeaveRequestAsync(existing);
                if (cancelled == null)
                {
                    return CreateError(call.Id, "Unable to cancel the leave request.");
                }

                return CreateSuccess(call.Id, "Leave request cancelled successfully.", cancelled);
 
            }
            catch (BusinessRuleException ex)
            {
                _logger.LogError(ex, ex.Message);
                return CreateError(call.Id, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CancelLeaveRequestToolHandler");
                return CreateError(call.Id, "An internal error occurred while cancelling the leave request.");
            }
        }
    }
}
