using HospitalSchedulingApp.Common.Enums;
using HospitalSchedulingApp.Dal.Entities;
using HospitalSchedulingApp.Dal.Repositories;
using HospitalSchedulingApp.Dtos.Shift.Requests;
using HospitalSchedulingApp.Dtos.Shift.Response;
using HospitalSchedulingApp.Services.Interfaces;

namespace HospitalSchedulingApp.Services
{
    public class ShiftSwapService : IShiftSwapService
    {
        private readonly IRepository<ShiftSwapRequest> _shiftSwapRepository;
        private readonly IRepository<PlannedShift> _plannedShiftRepo;
        private readonly IRepository<Staff> _staffRepository;
        private readonly IRepository<ShiftType> _shiftTypeRepository;
        private readonly IRepository<Department> _departmentRepository;

        public ShiftSwapService(IRepository<ShiftSwapRequest> shiftSwapRepository,
            IRepository<Staff> staffRepository,
            IRepository<ShiftType> shiftTypeRepository,
            IRepository<PlannedShift> plannedShiftRepo,
            IRepository<Department> departmentRepository)

        {
            _shiftSwapRepository = shiftSwapRepository;
            _staffRepository = staffRepository;
            _shiftTypeRepository = shiftTypeRepository;
            _plannedShiftRepo = plannedShiftRepo;
            _departmentRepository = departmentRepository;
        }



        public Task<ShiftSwapRequest> ApproveOrRejectShiftSwapRequest(ShiftSwapRequest request)
        {
            throw new NotImplementedException();
        }

        public async Task<ShiftSwapResponse> SubmitShiftSwapRequestAsync(ShiftSwapRequest request)
        {
            var shifts = await _plannedShiftRepo.GetAllAsync();

            var sourceShift =  shifts
                .Where(x =>
                    x.AssignedStaffId == request.RequestingStaffId &&
                    x.ShiftDate == request.SourceShiftDate &&
                    x.ShiftTypeId == (ShiftTypes)request.SourceShiftTypeId)
                .FirstOrDefault();

            if (sourceShift == null)
            {
                throw new Exception("Source shift does not exist for the requesting staff.");
            }

            var targetShift =  shifts
                .Where(x =>
                    x.AssignedStaffId == request.TargetStaffId &&
                    x.ShiftDate == request.TargetShiftDate &&
                    x.ShiftTypeId == (ShiftTypes)request.TargetShiftTypeId)
                .FirstOrDefault();

            if (targetShift == null)
            {
                throw new Exception("Target shift does not exist for the selected staff.");
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
                throw new Exception("A similar shift swap request already exists and is pending or approved.");
            }


            // Set request timestamp
            request.RequestedAt = DateTime.Now;

            // Save the request
            await _shiftSwapRepository.AddAsync(request);
            await _shiftSwapRepository.SaveAsync();



            // Fetch related data
            var requestingStaff = await _staffRepository.GetByIdAsync(request.RequestingStaffId);
            var targetStaff = await _staffRepository.GetByIdAsync(request.TargetStaffId);

            var sourceShiftType = await _shiftTypeRepository.GetByIdAsync(request.SourceShiftTypeId);
            var targetShiftType = await _shiftTypeRepository.GetByIdAsync(request.TargetShiftTypeId);


            // Build response
            return new ShiftSwapResponse
            {

                RequestingStaffId = request.RequestingStaffId,
                RequestingStaffName = requestingStaff?.StaffName ?? "Unknown",

                TargetStaffId = request.TargetStaffId,
                TargetStaffName = targetStaff?.StaffName ?? "Unknown",

                SourceShiftDate = request.SourceShiftDate,
                SourceShiftTypeId = request.SourceShiftTypeId,
                SourceShiftTypeName = sourceShiftType?.ShiftTypeName ?? "N/A",

                TargetShiftDate = request.TargetShiftDate,
                TargetShiftTypeId = request.TargetShiftTypeId,
                TargetShiftTypeName = targetShiftType?.ShiftTypeName ?? "N/A",

                StatusId = (int)request.StatusId,
                ShiftSwapStatus = "Pending",

                RequestedAt = request.RequestedAt,
            };
        }

        /// <summary>
        /// Asynchronously retrieves a list of shift swap requests filtered by the specified status.
        /// </summary>
        /// <param name="status">The status of the shift swap requests to retrieve. This parameter determines which requests are included in the result.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of  <see cref="ShiftSwapResponse"/> objects matching the specified status. If no requests match  the status, the list will be empty.</returns>
        public async Task<List<ShiftSwapResponse>> FetchShiftSwapRequestsAsync(ShiftSwapDto shiftSwapDto)
        {
            // Fetch shift swap requests from the repository
            // Fetch shift swap requests from the repository
            var shiftSwapRequests = (await _shiftSwapRepository.GetAllAsync())
                .Where(x => (shiftSwapDto.StatusId == null || x.StatusId == (ShiftSwapStatuses)shiftSwapDto.StatusId)
                     && (shiftSwapDto.requesterStaffId == null || shiftSwapDto.requesterStaffId == x.RequestingStaffId)
                     && (shiftSwapDto.targetStaffId == null || shiftSwapDto.targetStaffId == x.TargetStaffId)
                     && (shiftSwapDto.requesterShiftTypeId == null || shiftSwapDto.requesterShiftTypeId == x.SourceShiftTypeId)
                     && (shiftSwapDto.targetShiftTypeId == null || shiftSwapDto.targetShiftTypeId == x.TargetShiftTypeId)
                     && (shiftSwapDto.fromDate == null || x.SourceShiftDate >= shiftSwapDto.fromDate)
                     && (shiftSwapDto.toDate == null || x.TargetShiftDate <= shiftSwapDto.toDate)
                )
                .ToList(); // Materialize once for performance

            var staffRepo = (await _staffRepository.GetAllAsync()).ToDictionary(s => s.StaffId);
            var plannedShifts = (await _plannedShiftRepo.GetAllAsync()).ToList();
            // Replace the following lines inside FetchShiftSwapRequestsAsync:

            var plannedShiftLookup = plannedShifts
                .GroupBy(ps => new { ps.ShiftDate, ShiftTypeId = (int)ps.ShiftTypeId, ps.AssignedStaffId })
                .ToDictionary(
                    g => g.Key,
                    g => g.First()
                );
            var departments = (await _departmentRepository.GetAllAsync()).ToDictionary(d => d.DepartmentId);
            var shiftTypes = (await _shiftTypeRepository.GetAllAsync()).ToDictionary(st => st.ShiftTypeId);

            var shiftSwapResponses = new List<ShiftSwapResponse>(shiftSwapRequests.Count);

            foreach (var shiftSwap in shiftSwapRequests)
            {
                staffRepo.TryGetValue(shiftSwap.RequestingStaffId, out var requestingStaff);
                staffRepo.TryGetValue(shiftSwap.TargetStaffId, out var targetStaff);

                shiftTypes.TryGetValue(shiftSwap.SourceShiftTypeId, out var sourceShiftType);
                shiftTypes.TryGetValue(shiftSwap.TargetShiftTypeId, out var targetShiftType);

                // When creating sourceKey and targetKey, ensure AssignedStaffId is nullable (int?):
                var sourceKey = new { ShiftDate = shiftSwap.SourceShiftDate, ShiftTypeId = shiftSwap.SourceShiftTypeId, AssignedStaffId = (int?)shiftSwap.RequestingStaffId };
                var targetKey = new { ShiftDate = shiftSwap.TargetShiftDate, ShiftTypeId = shiftSwap.TargetShiftTypeId, AssignedStaffId = (int?)shiftSwap.TargetStaffId };

                plannedShiftLookup.TryGetValue(sourceKey, out var sourcePlannedShift);
                plannedShiftLookup.TryGetValue(targetKey, out var targetPlannedShift);

                var sourceDeptId = sourcePlannedShift?.DepartmentId ?? 0;
                var targetDeptId = targetPlannedShift?.DepartmentId ?? 0;

                departments.TryGetValue(sourceDeptId, out var sourceDept);
                departments.TryGetValue(targetDeptId, out var targetDept);

                shiftSwapResponses.Add(new ShiftSwapResponse
                {
                    RequestingStaffId = shiftSwap.RequestingStaffId,
                    RequestingStaffName = requestingStaff?.StaffName ?? "Unknown",
                    TargetStaffId = shiftSwap.TargetStaffId,
                    TargetStaffName = targetStaff?.StaffName ?? "Unknown",
                    SourceShiftDate = shiftSwap.SourceShiftDate,
                    SourceShiftTypeId = shiftSwap.SourceShiftTypeId,
                    SourceShiftTypeName = sourceShiftType?.ShiftTypeName ?? "N/A",
                    TargetShiftDate = shiftSwap.TargetShiftDate,
                    TargetShiftTypeId = shiftSwap.TargetShiftTypeId,
                    TargetShiftTypeName = targetShiftType?.ShiftTypeName ?? "N/A",
                    StatusId = (int)shiftSwap.StatusId,
                    SourceDepartmentId = sourceDeptId,
                    SourceDepartmentName = sourceDept?.DepartmentName ?? "N/A",
                    TargetDepartmentId = targetDeptId,
                    TargetDepartmentName = targetDept?.DepartmentName ?? "N/A",
                    ShiftSwapStatus = ((ShiftSwapStatuses)shiftSwap.StatusId).ToString(),
                    RequestedAt = shiftSwap.RequestedAt
                });
            }           


            return shiftSwapResponses;
        }
    }
}
