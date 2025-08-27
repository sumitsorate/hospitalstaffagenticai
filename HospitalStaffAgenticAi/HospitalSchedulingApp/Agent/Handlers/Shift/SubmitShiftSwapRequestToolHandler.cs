using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.Tools.Shift;
using HospitalSchedulingApp.Common.Enums;
using HospitalSchedulingApp.Common.Extensions;
using HospitalSchedulingApp.Common.Handlers;
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
    public class SubmitShiftSwapRequestToolHandler : BaseToolHandler
    {
        private readonly IShiftSwapService _shiftSwapService;
        private readonly IPlannedShiftService _plannedShiftService;
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
            : base(logger)
        {
            _shiftSwapService = shiftSwapService;
            _userContextService = userContextService;
            _plannedShiftService = plannedShiftService;
            _plannedShiftRepo = plannedShiftRepo;
            _shiftSwapRepository = shiftSwapRepository;
        }

        public override string ToolName => SubmitShiftSwapRequestTool.GetTool().Name;

        public override async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
        {
            try
            {
                // 🧑‍💼 Parse and validate input
                int? requestingStaffId = root.FetchInt("requestingStaffId");
                if (requestingStaffId is null || requestingStaffId <= 0)
                    return CreateError(call.Id, "❌ Missing or invalid requesting staff ID.");

                int? targetStaffId = root.FetchInt("targetStaffId");
                if (targetStaffId is null || targetStaffId <= 0)
                    return CreateError(call.Id, "❌ Missing or invalid target staff ID.");

                DateTime? sourceShiftDate = root.FetchDateTime("sourceShiftDate");
                if (sourceShiftDate is null)
                    return CreateError(call.Id, "❌ Invalid or missing source shift date (expected format: YYYY-MM-DD).");

                int? sourceShiftTypeId = root.FetchInt("sourceShiftTypeId");
                if (sourceShiftTypeId is null)
                    return CreateError(call.Id, "❌ Invalid or missing source shift type ID.");

                DateTime? targetShiftDate = root.FetchDateTime("targetShiftDate");
                if (targetShiftDate is null)
                    return CreateError(call.Id, "❌ Invalid or missing target shift date (expected format: YYYY-MM-DD).");

                int? targetShiftTypeId = root.FetchInt("targetShiftTypeId");
                if (targetShiftTypeId is null)
                    return CreateError(call.Id, "❌ Invalid or missing target shift type ID.");

                // 🔒 Permission check
                var isEmployee = _userContextService.IsEmployee();
                var loggedInUserStaffId = _userContextService.GetStaffId();

                if (isEmployee && requestingStaffId != loggedInUserStaffId)
                    return CreateError(call.Id, "🚫 You can only submit shift swap requests for yourself.");

                // 📝 Construct shift swap request object
                var request = new ShiftSwapRequest
                {
                    RequestingStaffId = requestingStaffId.Value,
                    TargetStaffId = targetStaffId.Value,
                    SourceShiftDate = sourceShiftDate.Value,
                    SourceShiftTypeId = sourceShiftTypeId.Value,
                    TargetShiftDate = targetShiftDate.Value,
                    TargetShiftTypeId = targetShiftTypeId.Value,
                    StatusId = ShiftSwapStatuses.Pending,
                    RequestedAt = DateTime.Now.Date
                };

                // 🔎 Validate source/target shifts
                var shifts = await _plannedShiftRepo.GetAllAsync();

                var sourceShift = shifts.FirstOrDefault(x =>
                    x.AssignedStaffId == request.RequestingStaffId &&
                    x.ShiftDate == request.SourceShiftDate &&
                    x.ShiftTypeId == (ShiftTypes)request.SourceShiftTypeId);

                if (sourceShift == null)
                    return CreateError(call.Id, "⚠️ No source shift found for the given date and type.");

                var targetShift = shifts.FirstOrDefault(x =>
                    x.AssignedStaffId == request.TargetStaffId &&
                    x.ShiftDate == request.TargetShiftDate &&
                    x.ShiftTypeId == (ShiftTypes)request.TargetShiftTypeId);

                if (targetShift == null)
                    return CreateError(call.Id, "⚠️ No target shift found for the given date and type.");

                // 🛡️ Check for duplicate requests
                var existingRequests = await _shiftSwapRepository.GetAllAsync();
                var isDuplicate = existingRequests.Any(x =>
                    x.RequestingStaffId == request.RequestingStaffId &&
                    x.TargetStaffId == request.TargetStaffId &&
                    x.SourceShiftDate == request.SourceShiftDate &&
                    x.SourceShiftTypeId == request.SourceShiftTypeId &&
                    x.TargetShiftDate == request.TargetShiftDate &&
                    x.TargetShiftTypeId == request.TargetShiftTypeId &&
                    x.StatusId != ShiftSwapStatuses.Rejected);

                if (isDuplicate)
                    return CreateError(call.Id, "⚠️ A similar shift swap request already exists and is pending or approved.");

                // 💾 Save the request
                await _shiftSwapService.SubmitShiftSwapRequestAsync(request);

                var response = new
                {
                    success = true,
                    message = $"✅Shift swap request submitted successfully",
                    data = request,
                };

                var json = JsonSerializer.Serialize(response);
                return new ToolOutput(call.Id, json);
              
            }
            catch (Exception ex)
            {
                return CreateError(call.Id,  "⚠️ An internal error occurred while submitting the shift swap request.");
            }
        }
    }
}
