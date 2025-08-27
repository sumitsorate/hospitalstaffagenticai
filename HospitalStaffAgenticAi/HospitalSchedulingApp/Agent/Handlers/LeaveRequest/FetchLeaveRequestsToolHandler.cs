using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.Tools.LeaveRequest;
using HospitalSchedulingApp.Common.Enums;
using HospitalSchedulingApp.Common.Exceptions;
using HospitalSchedulingApp.Common.Extensions;
using HospitalSchedulingApp.Common.Handlers;
using HospitalSchedulingApp.Dtos.LeaveRequest.Request;
using HospitalSchedulingApp.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.LeaveRequest
{
    /// <summary>
    /// 🎯 Tool handler to fetch leave requests.
    /// Supports filtering by staff, status, date range, and leave type.
    /// </summary>
    public class FetchLeaveRequestToolHandler : BaseToolHandler
    {
        private readonly ILeaveRequestService _leaveRequestService;

        /// <summary>
        /// Initializes a new instance of <see cref="FetchLeaveRequestToolHandler"/>.
        /// </summary>
        /// <param name="leaveRequestService">Service to manage leave requests.</param>
        /// <param name="logger">Logger instance for structured logging.</param>
        public FetchLeaveRequestToolHandler(
            ILeaveRequestService leaveRequestService,
            ILogger<FetchLeaveRequestToolHandler> logger)
            : base(logger)
        {
            _leaveRequestService = leaveRequestService
                ?? throw new ArgumentNullException(nameof(leaveRequestService));
        }

        /// <summary>
        /// Gets the tool name used by the AI agent runtime.
        /// </summary>
        public override string ToolName => FetchLeaveRequestTool.GetTool().Name;

        /// <summary>
        /// Handles the tool execution for fetching leave requests.
        /// </summary>
        /// <param name="call">The function tool call object.</param>
        /// <param name="root">The JSON input payload.</param>
        /// <returns>Tool output with leave request data or error response.</returns>
        public override async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            try
            {
                // 🔍 Parse optional filters
                int? leaveRequestId = root.FetchInt("leaveRequestId");
                int? staffId = root.FetchInt("staffId");

                // 📌 Leave status validation (enum)
                LeaveRequestStatuses? leaveStatusId = null;
                int? leaveStatusInt = root.FetchInt("leaveStatusId");
                if (leaveStatusInt.HasValue)
                {
                    if (!Enum.IsDefined(typeof(LeaveRequestStatuses), leaveStatusInt.Value))
                        throw new BusinessRuleException($"Invalid leave status ID: {leaveStatusInt.Value}");

                    leaveStatusId = (LeaveRequestStatuses)leaveStatusInt.Value;
                }

                // 📅 Date range validation
                DateTime? startDate = root.FetchDateTime("startDate");
                DateTime? endDate = root.FetchDateTime("endDate");
                if (startDate.HasValue && endDate.HasValue && startDate > endDate)
                    throw new BusinessRuleException("Start date cannot be after end date.");

                // 🏷 Leave type validation (enum)
                LeaveType? leaveTypeId = null;
                int? leaveTypeInt = root.FetchInt("leaveTypeId");
                if (leaveTypeInt.HasValue)
                {
                    if (!Enum.IsDefined(typeof(LeaveType), leaveTypeInt.Value))
                        throw new BusinessRuleException($"Invalid leave type ID: {leaveTypeInt.Value}");

                    leaveTypeId = (LeaveType)leaveTypeInt.Value;
                }

                // 📝 Build filter object
                var filter = new LeaveRequestFilter
                {
                    LeaveRequestId = leaveRequestId,
                    StaffId = staffId,
                    LeaveStatusId = leaveStatusId,
                    StartDate = startDate,
                    EndDate = endDate,
                    LeaveTypeId = leaveTypeId
                };

                _logger.LogInformation("Fetching leave requests with filter: {@Filter}", filter);

                // 🚀 Execute service call
                var leaveRequests = await _leaveRequestService.FetchLeaveRequestsAsync(filter);

                // ✅ Return success response
                return CreateSuccess(call.Id, "Leave requests fetched successfully.", leaveRequests);
            }
            catch (BusinessRuleException brEx)
            {
                // ⚠️ Known business validation failure
                _logger.LogWarning(brEx, "Business rule validation failed in FetchLeaveRequestToolHandler");
                return CreateError(call.Id, $"⚠️ {brEx.Message}");
            }
            catch (Exception ex)
            {
                // ❌ Unexpected technical failure
                _logger.LogError(ex, "Unhandled exception in FetchLeaveRequestToolHandler");
                return CreateError(call.Id, "❌ An internal error occurred while fetching leave requests.");
            }
        }
    }
}
