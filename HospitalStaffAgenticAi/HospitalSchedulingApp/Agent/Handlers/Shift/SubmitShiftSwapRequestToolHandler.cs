using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.Tools.Shift;
using HospitalSchedulingApp.Common.Enums;
using HospitalSchedulingApp.Dal.Entities;
using HospitalSchedulingApp.Dal.Repositories;
using HospitalSchedulingApp.Services.AuthServices.Interfaces;
using HospitalSchedulingApp.Services.Interfaces;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Handlers.Shift
{
    /// <summary>
    /// 🔁 Handler for submitting a shift swap request using the SubmitShiftSwapRequestTool.
    /// </summary>
    public class SubmitShiftSwapRequestToolHandler : IToolHandler
    {
        private readonly IShiftSwapService _shiftSwapService;
        private readonly IPlannedShiftService _plannedShiftService;
        private readonly ILogger<SubmitShiftSwapRequestToolHandler> _logger;
        private readonly IUserContextService _userContextService;
        private readonly IRepository<PlannedShift> _plannedShiftRepo;
        private readonly IRepository<ShiftSwapRequest> _shiftSwapRepository;

        public SubmitShiftSwapRequestToolHandler(
            IShiftSwapService shiftSwapService,
            ILogger<SubmitShiftSwapRequestToolHandler> logger,
            IUserContextService userContextService,
            IPlannedShiftService plannedShiftService,
            IRepository<PlannedShift> plannedShiftRepo,
            IRepository<ShiftSwapRequest> shiftSwapRepository)
        {
            _shiftSwapService = shiftSwapService;
            _logger = logger;
            _userContextService = userContextService;
            _plannedShiftService = plannedShiftService;
            _plannedShiftRepo = plannedShiftRepo;
            _shiftSwapRepository = shiftSwapRepository;
        }

        public string ToolName => SubmitShiftSwapRequestTool.GetTool().Name;

        public async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            try
            {
                // 🧑‍💼 Parse and validate input
                if (!root.TryGetProperty("requestingStaffId", out var requestingStaffIdProp) ||
                    !requestingStaffIdProp.TryGetInt32(out var requestingStaffId) || requestingStaffId <= 0)
                    return CreateError(call.Id, "❌ Missing or invalid requesting staff ID.");

                if (!root.TryGetProperty("targetStaffId", out var targetStaffIdProp) ||
                    !targetStaffIdProp.TryGetInt32(out var targetStaffId) || targetStaffId <= 0)
                    return CreateError(call.Id, "❌ Missing or invalid target staff ID.");

                if (!root.TryGetProperty("sourceShiftDate", out var sourceDateProp) ||
                    !DateTime.TryParse(sourceDateProp.GetString(), out var sourceShiftDate))
                    return CreateError(call.Id, "❌ Invalid or missing source shift date (expected format: YYYY-MM-DD).");

                if (!root.TryGetProperty("sourceShiftTypeId", out var sourceTypeProp) ||
                    !sourceTypeProp.TryGetInt32(out var sourceShiftTypeId))
                    return CreateError(call.Id, "❌ Invalid or missing source shift type ID.");

                if (!root.TryGetProperty("targetShiftDate", out var targetDateProp) ||
                    !DateTime.TryParse(targetDateProp.GetString(), out var targetShiftDate))
                    return CreateError(call.Id, "❌ Invalid or missing target shift date (expected format: YYYY-MM-DD).");

                if (!root.TryGetProperty("targetShiftTypeId", out var targetTypeProp) ||
                    !targetTypeProp.TryGetInt32(out var targetShiftTypeId))
                    return CreateError(call.Id, "❌ Invalid or missing target shift type ID.");

                // 🔒 Permission check
                var isEmployee = _userContextService.IsEmployee();
                var loggedInUserStaffId = _userContextService.GetStaffId();

                if (isEmployee && requestingStaffId != loggedInUserStaffId)
                    return CreateError(call.Id, "🚫 You can only submit shift swap requests for yourself.");

                // 📝 Construct shift swap request object
                var request = new ShiftSwapRequest
                {
                    RequestingStaffId = requestingStaffId,
                    TargetStaffId = targetStaffId,
                    SourceShiftDate = sourceShiftDate,
                    SourceShiftTypeId = sourceShiftTypeId,
                    TargetShiftDate = targetShiftDate,
                    TargetShiftTypeId = targetShiftTypeId,
                    StatusId = Common.Enums.ShiftSwapStatuses.Pending,
                    RequestedAt = DateTime.UtcNow
                };

                var shifts = await _plannedShiftRepo.GetAllAsync();

                var sourceShift = shifts
                    .Where(x =>
                        x.AssignedStaffId == request.RequestingStaffId &&
                        x.ShiftDate == request.SourceShiftDate &&
                        x.ShiftTypeId == (ShiftTypes)request.SourceShiftTypeId)
                    .FirstOrDefault();

                if (sourceShift == null)
                {
                    return CreateError(call.Id, "⚠️ No Source shift found for the given date and type.");
                }

                var targetShift = shifts
                    .Where(x =>
                        x.AssignedStaffId == request.TargetStaffId &&
                        x.ShiftDate == request.TargetShiftDate &&
                        x.ShiftTypeId == (ShiftTypes)request.TargetShiftTypeId)
                    .FirstOrDefault();

                if (targetShift == null)
                {
                    return CreateError(call.Id, "⚠️ No target shift found for the given date and type.");
                }


                // 🛡️ Check for duplicate request
                var existingRequests = await _shiftSwapRepository.GetAllAsync();
                var isDuplicate = existingRequests.Any(x =>
                    x.RequestingStaffId == request.RequestingStaffId &&
                    x.TargetStaffId == request.TargetStaffId &&
                    x.SourceShiftDate == request.SourceShiftDate &&
                    x.SourceShiftTypeId == request.SourceShiftTypeId &&
                    x.TargetShiftDate == request.TargetShiftDate &&
                    x.TargetShiftTypeId == request.TargetShiftTypeId &&
                    x.StatusId != ShiftSwapStatuses.Rejected  // Optional: allow retry if rejected
                );

                if (isDuplicate)
                {
                    return CreateError(call.Id, "⚠️ A similar shift swap request already exists and is pending or approved..");                   
                }

                // 💾 Save the request
                await _shiftSwapService.SubmitShiftSwapRequestAsync(request);

                // ✅ Respond with success
                var response = new
                {
                    success = true,
                    message = "✅ Shift swap request submitted successfully!",
                    data = new
                    {
                        request.RequestingStaffId,
                        request.TargetStaffId,
                        request.SourceShiftDate,
                        request.SourceShiftTypeId,
                        request.TargetShiftDate,
                        request.TargetShiftTypeId,
                        status = ShiftSwapStatuses.Pending
                    }
                };

                _logger.LogInformation("✅ Shift swap request submitted by StaffId {Requesting} for shift {SourceShiftDate} <-> {TargetShiftDate} with {Target}.",
                    requestingStaffId, sourceShiftDate.ToShortDateString(), targetShiftDate.ToShortDateString(), targetStaffId);

                return new ToolOutput(call.Id, JsonSerializer.Serialize(response));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❗ Error occurred while handling shift swap request.");
                return CreateError(call.Id, "⚠️ An internal error occurred while submitting the shift swap request.");
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
