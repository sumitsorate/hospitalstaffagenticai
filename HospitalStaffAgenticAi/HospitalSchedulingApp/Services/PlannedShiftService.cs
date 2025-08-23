using HospitalSchedulingApp.Common;
using HospitalSchedulingApp.Common.Enums;
using HospitalSchedulingApp.Common.Exceptions;
using HospitalSchedulingApp.Dal.Entities;
using HospitalSchedulingApp.Dal.Repositories;
using HospitalSchedulingApp.Dtos.LeaveRequest.Request;
using HospitalSchedulingApp.Dtos.Shift.Requests;
using HospitalSchedulingApp.Dtos.Shift.Response;
using HospitalSchedulingApp.Services.AuthServices.Interfaces;
using HospitalSchedulingApp.Services.Interfaces;

namespace HospitalSchedulingApp.Services
{
    public class PlannedShiftService : IPlannedShiftService
    {
        private readonly IRepository<PlannedShift> _plannedShiftRepo;
        private readonly IRepository<Department> _departmentRepo;
        private readonly IRepository<ShiftType> _shiftTypeRepo;
        private readonly IRepository<ShiftStatus> _shiftStatusRepo;
        private readonly IRepository<Staff> _staffRepo;
        private readonly IUserContextService _userContextService;
        private readonly ILeaveRequestService _leaveRequestService;
        public PlannedShiftService(
            IRepository<PlannedShift> plannedShiftRepo,
            IRepository<Department> departmentRepo,
            IRepository<ShiftType> shiftTypeRepo,
            IRepository<ShiftStatus> shiftStatusRepo,
            IRepository<Staff> staffRepo,
            IUserContextService userContextService,
            ILeaveRequestService leaveRequestService)
        {
            _plannedShiftRepo = plannedShiftRepo;
            _departmentRepo = departmentRepo;
            _shiftTypeRepo = shiftTypeRepo;
            _shiftStatusRepo = shiftStatusRepo;
            _staffRepo = staffRepo;
            _userContextService = userContextService;
            _leaveRequestService = leaveRequestService;
        }

        /// <summary>
        /// Fetches planned shifts with optional filtering and applies user visibility rules.
        /// </summary>
        /// <param name="filter">Filter criteria for planned shifts.</param>
        /// <returns>List of planned shift details matching the filter.</returns>
        /// <exception cref="BusinessRuleException">
        /// Thrown if an employee attempts to view another staff member's shifts.
        /// </exception>
        public async Task<List<PlannedShiftDetailDto>> FetchFilteredPlannedShiftsAsync(ShiftFilterDto filter)
        {
            // 🔐 Restrict employee visibility
            var isEmployee = _userContextService.IsEmployee();
            var loggedInUserStaffID = _userContextService.GetStaffId();

            if (isEmployee)
            {
                if (filter.StaffId.HasValue && filter.StaffId != loggedInUserStaffID)
                    throw new BusinessRuleException("🚫 You're only allowed to view your own shift schedule.");

                // Force staff filter to logged-in user
                filter.StaffId = loggedInUserStaffID;
            }

            // Load reference data (convert to dictionaries for faster lookups)
            var shifts = await _plannedShiftRepo.GetAllAsync();
            var departments = (await _departmentRepo.GetAllAsync()).ToDictionary(d => d.DepartmentId);
            var shiftTypes = (await _shiftTypeRepo.GetAllAsync()).ToDictionary(st => st.ShiftTypeId);
            var shiftStatuses = (await _shiftStatusRepo.GetAllAsync()).ToDictionary(ss => ss.ShiftStatusId);
            var staff = (await _staffRepo.GetAllAsync()).ToDictionary(s => s.StaffId);

            // Apply filters (ideally these should be pushed to DB instead of LINQ-to-objects)
            if (filter.PlannedShiftId.HasValue)
                shifts = shifts.Where(s => s.PlannedShiftId == filter.PlannedShiftId.Value).ToList();

            if (filter.SlotNumber.HasValue)
                shifts = shifts.Where(s => s.SlotNumber == filter.SlotNumber.Value).ToList();

            if (filter.FromDate.HasValue)
                shifts = shifts.Where(s => s.ShiftDate >= filter.FromDate.Value).ToList();

            if (filter.ToDate.HasValue)
                shifts = shifts.Where(s => s.ShiftDate <= filter.ToDate.Value).ToList();

            if (filter.DepartmentId.HasValue)
                shifts = shifts.Where(s => s.DepartmentId == filter.DepartmentId.Value).ToList();

            if (filter.ShiftTypeId.HasValue)
                shifts = shifts.Where(s => (int)s.ShiftTypeId == filter.ShiftTypeId.Value).ToList();

            if (filter.ShiftStatusId.HasValue)
                shifts = shifts.Where(s => (int)s.ShiftStatusId == filter.ShiftStatusId.Value).ToList();

            if (filter.StaffId.HasValue)
                shifts = shifts.Where(s => s.AssignedStaffId == filter.StaffId.Value).ToList();

            // Map to DTOs
            var dtos = shifts.Select(shift =>
            {
                staff.TryGetValue(shift.AssignedStaffId ?? -1, out var assignedStaff);

                return new PlannedShiftDetailDto
                {
                    PlannedShiftId = shift.PlannedShiftId,
                    ShiftDate = shift.ShiftDate,
                    SlotNumber = shift.SlotNumber,
                    ShiftTypeId = (int)shift.ShiftTypeId,
                    DepartmentId = shift.DepartmentId,
                    ShiftStatusId = (int)shift.ShiftStatusId,
                    AssignedStaffId = shift.AssignedStaffId,

                    ShiftTypeName = shiftTypes.TryGetValue((int)shift.ShiftTypeId, out var st) ? st.ShiftTypeName : string.Empty,
                    ShiftDeparmentName = departments.TryGetValue(shift.DepartmentId, out var d) ? d.DepartmentName : string.Empty,
                    ShiftStatusName = shiftStatuses.TryGetValue((int)shift.ShiftStatusId, out var ss) ? ss.ShiftStatusName : string.Empty,
                    AssignedStaffFullName = assignedStaff?.StaffName ?? string.Empty,
                    AssignedStaffDepartmentName = assignedStaff != null && departments.TryGetValue(assignedStaff.StaffDepartmentId, out var ad)
                        ? ad.DepartmentName
                        : string.Empty
                };
            })
            .GroupBy(s => s.PlannedShiftId)   // Ensure uniqueness if needed
            .Select(g => g.First())
            .OrderBy(s => s.ShiftDate)
            .ThenBy(s => s.ShiftTypeId)
            .ThenBy(s => s.SlotNumber)
            .ToList();

            return dtos;
        }


        /// <summary>
        /// Assigns a staff member to a planned shift, ensuring no conflicts with existing assignments or leave requests.
        /// </summary>
        /// <param name="plannedShiftId">The ID of the planned shift.</param>
        /// <param name="staffId">The ID of the staff member to assign.</param>
        /// <returns>
        /// A <see cref="PlannedShiftDto"/> with updated assignment details,
        /// or throws <see cref="BusinessRuleException"/> if validation fails.
        /// </returns>
        /// <exception cref="BusinessRuleException">
        /// Thrown if the shift is not found, already assigned, staff is unavailable due to leave,
        /// or if there is a scheduling conflict.
        /// </exception>
        public async Task<PlannedShiftDto?> AssignedShiftToStaffAsync(int plannedShiftId, int staffId)
        {
            // 📋 Fetch shift info with staff candidate
            var shiftFilter = new ShiftFilterDto { PlannedShiftId = plannedShiftId };
            var shiftInfo = await FetchFilteredPlannedShiftsAsync(shiftFilter);

            var firstShift = shiftInfo?.FirstOrDefault();
            if (firstShift == null)
                throw new BusinessRuleException("❌ Shift information not found.");

            // 🚫 Already assigned checks
            if (firstShift.AssignedStaffId == staffId)
                throw new BusinessRuleException("❌ The same staff member is already assigned to this shift.");

            if (firstShift.AssignedStaffId.HasValue && firstShift.AssignedStaffId != staffId)
                throw new BusinessRuleException($"❌ Shift is already assigned to another staff member (ID {firstShift.AssignedStaffId}).");

            // 📆 Check leave conflicts (pending or approved)
            var overlappingLeaves = await _leaveRequestService.FetchLeaveRequestsAsync(new LeaveRequestFilter
            {
                StaffId = staffId,
                StartDate = firstShift.ShiftDate,
                EndDate = firstShift.ShiftDate
            });

            if (overlappingLeaves?.Any(lr =>
                lr.LeaveStatus == LeaveRequestStatuses.Approved ||
                lr.LeaveStatus == LeaveRequestStatuses.Pending) == true)
            {
                throw new BusinessRuleException(
                    $"❌ Staff ID {staffId} has a leave (pending/approved) on {firstShift.ShiftDate:yyyy-MM-dd}.");
            }

            // 🔄 Prevent duplicate assignment to same slot/type
            var existingShifts = await FetchFilteredPlannedShiftsAsync(new ShiftFilterDto
            {
                FromDate = firstShift.ShiftDate,
                ToDate = firstShift.ShiftDate,
                StaffId = staffId
            });

            if (existingShifts.Any(s =>
                    s.PlannedShiftId != plannedShiftId &&
                    s.ShiftTypeId == firstShift.ShiftTypeId &&
                    s.SlotNumber == firstShift.SlotNumber))
            {
                throw new BusinessRuleException(
                    $"❌ Staff ID {staffId} is already assigned to another shift at the same time.");
            }

            // ✅ Fetch the actual shift entity
            var shift = await _plannedShiftRepo.GetByIdAsync(plannedShiftId);
            if (shift == null)
                throw new BusinessRuleException($"❌ Planned shift with ID {plannedShiftId} not found.");

            // 🔄 Assign staff
            shift.AssignedStaffId = staffId;
            shift.ShiftStatusId = ShiftStatuses.Scheduled;

            _plannedShiftRepo.Update(shift);
            await _plannedShiftRepo.SaveAsync();

            // 📦 Fetch related metadata concurrently
            var shiftTypeTask = _shiftTypeRepo.GetByIdAsync((int)shift.ShiftTypeId);
            var departmentTask = _departmentRepo.GetByIdAsync(shift.DepartmentId);
            var staffTask = _staffRepo.GetByIdAsync(staffId);

            await Task.WhenAll(shiftTypeTask, departmentTask, staffTask);

            // 🎯 Map to DTO
            return new PlannedShiftDto
            {
                PlannedShiftId = shift.PlannedShiftId,
                ShiftDate = shift.ShiftDate,
                SlotNumber = shift.SlotNumber,
                ShiftTypeName = shiftTypeTask.Result?.ShiftTypeName ?? "N/A",
                ShiftDeparmentName = departmentTask.Result?.DepartmentName ?? "N/A",
                AssignedStaffFullName = staffTask.Result?.StaffName ?? "Unknown"
            };
        }


        /// <summary>
        /// Adds a new planned shift to the schedule. 
        /// By default, the shift will be vacant (unassigned).
        /// </summary>
        /// <param name="plannedShift">The planned shift entity to add.</param>
        /// <returns>A <see cref="PlannedShiftDto"/> representing the created shift.</returns>
        public async Task<PlannedShiftDto?> AddNewPlannedShiftAsync(PlannedShift plannedShift)
        {
            // Default: shift is vacant and unassigned
            plannedShift.AssignedStaffId = null;
            plannedShift.ShiftStatusId = ShiftStatuses.Vacant;

            // Save entity
            await _plannedShiftRepo.AddAsync(plannedShift);
            await _plannedShiftRepo.SaveAsync();

            // Fetch related metadata in parallel
            var departmentTask = _departmentRepo.GetByIdAsync(plannedShift.DepartmentId);
            var shiftTypeTask = _shiftTypeRepo.GetByIdAsync((int)plannedShift.ShiftTypeId);

            await Task.WhenAll(departmentTask, shiftTypeTask);

            var department = departmentTask.Result;
            var shiftType = shiftTypeTask.Result;

            // Validate existence (optional, depending on business rules)
            if (department == null)
                throw new BusinessRuleException($"Invalid DepartmentId: {plannedShift.DepartmentId}");
            if (shiftType == null)
                throw new BusinessRuleException($"Invalid ShiftTypeId: {plannedShift.ShiftTypeId}");

            // Map to DTO
            return new PlannedShiftDto
            {
                PlannedShiftId = plannedShift.PlannedShiftId,
                ShiftDate = plannedShift.ShiftDate,
                SlotNumber = plannedShift.SlotNumber,
                ShiftTypeName = shiftType.ShiftTypeName,
                ShiftDeparmentName = department.DepartmentName,
                AssignedStaffFullName = string.Empty // Always unassigned initially
            };
        }


        // Calendar UI Call
        public async Task<List<PlannedShiftDto>> FetchPlannedShiftsAsync(DateTime startDate, DateTime endDate)
        {
            var loggedInUserStaffId = _userContextService.GetStaffId();
            var isEmployee = _userContextService.IsEmployee();
            var isScheduler = _userContextService.IsScheduler();

            var shifts = await _plannedShiftRepo.GetAllAsync();
            var departments = await _departmentRepo.GetAllAsync();
            var shiftTypes = await _shiftTypeRepo.GetAllAsync();
            var staff = await _staffRepo.GetAllAsync();

            // Filter shifts by date
            var filteredShifts = shifts
                .Where(s => s.ShiftDate >= startDate && s.ShiftDate <= endDate);

            // Role-based filter
            if (isEmployee)
            {
                filteredShifts = filteredShifts
                    .Where(s => s.AssignedStaffId == loggedInUserStaffId);
            }

            // Convert to list after filtering
            var finalShiftList = filteredShifts
                .OrderBy(s => s.ShiftDate)
                .ToList();


            // Map to DTOs
            var dtos = finalShiftList.Select(shift =>
            {
                var staffMember = shift.AssignedStaffId.HasValue
                    ? staff.FirstOrDefault(s => s.StaffId == shift.AssignedStaffId.Value)
                    : null;

                return new PlannedShiftDto
                {
                    PlannedShiftId = shift.PlannedShiftId,
                    ShiftDate = shift.ShiftDate,
                    SlotNumber = shift.SlotNumber,
                    ShiftStatusId = (int)shift.ShiftStatusId,
                    ShiftTypeName = shiftTypes.FirstOrDefault(st => st.ShiftTypeId == (int)shift.ShiftTypeId)?.ShiftTypeName ?? string.Empty,
                    AssignedStaffFullName = staffMember?.StaffName ?? string.Empty,
                    ShiftDeparmentName = departments.FirstOrDefault(d => d.DepartmentId == shift.DepartmentId)?.DepartmentName ?? string.Empty
                };
            }).ToList();

            return dtos;
        }

        // Chat Interface call
        public async Task<List<PlannedShiftDetailDto>> FetchDetailedPlannedShiftsAsync(DateTime startDate, DateTime endDate)
        {
            var shifts = await _plannedShiftRepo.GetAllAsync();
            var departments = await _departmentRepo.GetAllAsync();
            var shiftTypes = await _shiftTypeRepo.GetAllAsync();
            var shiftStatuses = await _shiftStatusRepo.GetAllAsync();
            var staff = await _staffRepo.GetAllAsync();

            var filteredShifts = shifts
                .Where(s => s.ShiftDate >= startDate && s.ShiftDate <= endDate)
                .OrderBy(s => s.ShiftDate)
                .ToList();

            var dtos = filteredShifts.Select(shift =>
            {
                var assignedStaff = shift.AssignedStaffId.HasValue
                    ? staff.FirstOrDefault(s => s.StaffId == shift.AssignedStaffId.Value)
                    : null;

                return new PlannedShiftDetailDto
                {
                    PlannedShiftId = shift.PlannedShiftId,

                    // Core values
                    ShiftDate = shift.ShiftDate,
                    SlotNumber = shift.SlotNumber,

                    // IDs
                    ShiftTypeId = (int)shift.ShiftTypeId,
                    DepartmentId = shift.DepartmentId,
                    ShiftStatusId = (int)shift.ShiftStatusId,
                    AssignedStaffId = shift.AssignedStaffId,

                    // Resolved names
                    ShiftTypeName = shiftTypes.FirstOrDefault(st => st.ShiftTypeId == (int)shift.ShiftTypeId)?.ShiftTypeName ?? string.Empty,
                    ShiftDeparmentName = departments.FirstOrDefault(d => d.DepartmentId == shift.DepartmentId)?.DepartmentName ?? string.Empty,
                    ShiftStatusName = shiftStatuses.FirstOrDefault(ss => ss.ShiftStatusId == (int)shift.ShiftStatusId)?.ShiftStatusName ?? string.Empty,
                    AssignedStaffFullName = assignedStaff?.StaffName ?? string.Empty,
                    AssignedStaffDepartmentName = assignedStaff != null
                        ? departments.FirstOrDefault(d => d.DepartmentId == assignedStaff.StaffDepartmentId)?.DepartmentName ?? string.Empty
                        : string.Empty
                };
            }).ToList();


            return dtos;
        }

        /// <summary>
        /// Unassigns a staff member from a planned shift, marking it as vacant.
        /// </summary>
        /// <param name="plannedShiftId">The unique identifier of the planned shift.</param>
        /// <returns>
        public async Task<PlannedShiftDto?> UnassignedShiftFromStaffAsync(int plannedShiftId)
        {
            // Fetch shift directly by ID
            var shift = await _plannedShiftRepo.GetByIdAsync(plannedShiftId);
            if (shift == null)
            {
                throw new BusinessRuleException($"Planned shift with ID {plannedShiftId} not found.");
            }

            // Unassign the staff
            shift.AssignedStaffId = null;
            shift.ShiftStatusId = ShiftStatuses.Vacant;

            _plannedShiftRepo.Update(shift);
            await _plannedShiftRepo.SaveAsync();

            // Fetch related metadata in parallel
            var shiftTypeTask = _shiftTypeRepo.GetByIdAsync((int)shift.ShiftTypeId);
            var departmentTask = _departmentRepo.GetByIdAsync(shift.DepartmentId);

            await Task.WhenAll(shiftTypeTask, departmentTask);

            // Map to DTO
            return new PlannedShiftDto
            {
                PlannedShiftId = shift.PlannedShiftId,
                ShiftDate = shift.ShiftDate,
                SlotNumber = shift.SlotNumber,
                ShiftTypeName = shiftTypeTask.Result?.ShiftTypeName ?? string.Empty,
                ShiftDeparmentName = departmentTask.Result?.DepartmentName ?? string.Empty,
                AssignedStaffFullName = string.Empty
            };
        }

    }
}
