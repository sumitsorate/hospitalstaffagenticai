using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.Tools.LeaveRequest;
using HospitalSchedulingApp.Common.Enums;
using HospitalSchedulingApp.Common.Exceptions;
using HospitalSchedulingApp.Common.Extensions;
using HospitalSchedulingApp.Common.Handlers;
using HospitalSchedulingApp.Dal.Entities;
using HospitalSchedulingApp.Services.AuthServices.Interfaces;
using HospitalSchedulingApp.Services.Interfaces;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.LeaveRequest
{
    /// <summary>
    /// Handler for submitting a leave request via the <c>SubmitLeaveRequestTool</c>.
    /// </summary>
    public class SubmitLeaveRequestToolHandler : BaseToolHandler
    {
        private readonly ILeaveRequestService _leaveRequestService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubmitLeaveRequestToolHandler"/> class.
        /// </summary>
        /// <param name="leaveRequestService">Service for handling leave requests.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="userContextService">Service for accessing the user context (currently unused).</param>
        public SubmitLeaveRequestToolHandler(
            ILeaveRequestService leaveRequestService,
            ILogger<SubmitLeaveRequestToolHandler> logger,
            IUserContextService userContextService)
            : base(logger)
        {
            _leaveRequestService = leaveRequestService;
        }

        /// <inheritdoc/>
        public override string ToolName => SubmitLeaveRequestTool.GetTool().Name;

        /// <summary>
        /// Handles the submission of a leave request.
        /// Validates input parameters, constructs a <see cref="LeaveRequests"/> entity,
        /// and calls the <see cref="ILeaveRequestService"/> to persist the request.
        /// </summary>
        /// <param name="call">The tool call metadata.</param>
        /// <param name="root">The input payload as JSON.</param>
        /// <returns>
        /// A <see cref="ToolOutput"/> indicating:
        /// <list type="bullet">
        /// <item><description>✅ Success when the leave request is submitted successfully.</description></item>
        /// <item><description>❌ Validation error if inputs are invalid.</description></item>
        /// <item><description>⚠️ Internal error for unexpected failures.</description></item>
        /// </list>
        /// </returns>
        public override async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            try
            {
                // 🧑‍💼 Validate staff ID
                int? staffId = root.FetchInt("staffId");
                if (staffId is null or <= 0)
                    return CreateError(call.Id, "❌ Missing or invalid staff ID.");

                // 📆 Validate leave start date
                DateTime? leaveStart = root.FetchDateTime("leaveStart");
                if (leaveStart is null)
                    return CreateError(call.Id, "❌ Invalid or missing leave start date (expected format: YYYY-MM-DD).");

                // 📆 Validate leave end date
                DateTime? leaveEnd = root.FetchDateTime("leaveEnd");
                if (leaveEnd is null)
                    return CreateError(call.Id, "❌ Invalid or missing leave end date (expected format: YYYY-MM-DD).");

                if (leaveEnd < leaveStart)
                    return CreateError(call.Id, "⚠️ Leave end date must be on or after the start date.");

                // 🏷️ Validate leave type
                int? leaveTypeIdInt = root.FetchInt("leaveTypeId");
                if (leaveTypeIdInt is null || !Enum.IsDefined(typeof(LeaveType), leaveTypeIdInt.Value))
                    return CreateError(call.Id, "❌ Invalid or missing leave type ID. Please select a valid type.");

                var leaveTypeId = (LeaveType)leaveTypeIdInt.Value;

                // 📝 Construct leave request entity
                var leaveRequest = new LeaveRequests
                {
                    StaffId = staffId.Value,
                    LeaveStart = leaveStart.Value,
                    LeaveEnd = leaveEnd.Value,
                    LeaveTypeId = leaveTypeId,
                    LeaveStatusId = LeaveRequestStatuses.Pending
                };

                // ✅ Submit leave request
                var savedRequest = await _leaveRequestService.SubmitLeaveRequestAsync(leaveRequest);
                return CreateSuccess(call.Id, "✅ Leave request submitted successfully!", savedRequest);
            }
            catch (BusinessRuleException ex)
            {
                return CreateError(call.Id, $"❌ {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❗ Error in SubmitLeaveRequestToolHandler");
                return CreateError(call.Id, "⚠️ An internal error occurred while submitting the leave request.");
            }
        }
    }
}
