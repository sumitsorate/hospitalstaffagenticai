using HospitalSchedulingApp.Common;
using HospitalSchedulingApp.Common.Enums;
using HospitalSchedulingApp.Dal.Entities;
using HospitalSchedulingApp.Dal.Repositories;
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

        public PlannedShiftService(
            IRepository<PlannedShift> plannedShiftRepo,
            IRepository<Department> departmentRepo,
            IRepository<ShiftType> shiftTypeRepo,
            IRepository<ShiftStatus> shiftStatusRepo,
            IRepository<Staff> staffRepo,
            IUserContextService userContextService)
        {
            _plannedShiftRepo = plannedShiftRepo;
            _departmentRepo = departmentRepo;
            _shiftTypeRepo = shiftTypeRepo;
            _shiftStatusRepo = shiftStatusRepo;
            _staffRepo = staffRepo;
            _userContextService = userContextService;
        }


        // Calendar UI Call
        public async Task<PlannedShiftDto?> AddNewPlannedShiftAsync(PlannedShift plannedShift)
        {
            // Ensure shift is marked as vacant by default
            plannedShift.AssignedStaffId = null;
            plannedShift.ShiftStatusId = ShiftStatuses.Vacant;

            // Save to database
            await _plannedShiftRepo.AddAsync(plannedShift);
            await _plannedShiftRepo.SaveAsync();

            // Load metadata
            var department = await _departmentRepo.GetByIdAsync(plannedShift.DepartmentId);
            var shiftType = await _shiftTypeRepo.GetByIdAsync((int)plannedShift.ShiftTypeId);

            // Return DTO
            return new PlannedShiftDto
            {
                PlannedShiftId = plannedShift.PlannedShiftId,
                ShiftDate = plannedShift.ShiftDate,
                SlotNumber = plannedShift.SlotNumber,
                ShiftTypeName = shiftType?.ShiftTypeName ?? string.Empty,
                AssignedStaffFullName = "", // No one assigned
                ShiftDeparmentName = department?.DepartmentName ?? string.Empty
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

        public async Task<List<PlannedShiftDetailDto>> FetchFilteredPlannedShiftsAsync(ShiftFilterDto filter)
        {
            var shifts = await _plannedShiftRepo.GetAllAsync();
            var departments = await _departmentRepo.GetAllAsync();
            var shiftTypes = await _shiftTypeRepo.GetAllAsync();
            var shiftStatuses = await _shiftStatusRepo.GetAllAsync();
            var staff = await _staffRepo.GetAllAsync();

            // Filter by date range
            if (filter.PlannedShiftId.HasValue)
                shifts = shifts.Where(s => s.PlannedShiftId == filter.PlannedShiftId.Value).ToList();

            // Filter by date range
            if (filter.SlotNumber.HasValue)
                shifts = shifts.Where(s => s.SlotNumber == filter.SlotNumber.Value).ToList();

            // Filter by date range
            if (filter.FromDate.HasValue)
                shifts = shifts.Where(s => s.ShiftDate >= filter.FromDate.Value).ToList();

            if (filter.ToDate.HasValue)
                shifts = shifts.Where(s => s.ShiftDate <= filter.ToDate.Value).ToList();

            // Filter by department ID
            if (filter.DepartmentId.HasValue)
            {
                shifts = shifts.Where(s => s.DepartmentId == filter.DepartmentId.Value).ToList();
            }

            // Filter by shift type ID
            if (filter.ShiftTypeId.HasValue)
            {
                shifts = shifts.Where(s => (int)s.ShiftTypeId == filter.ShiftTypeId.Value).ToList();
            }

            // Filter by shift status ID
            if (filter.ShiftStatusId.HasValue)
            {
                shifts = shifts.Where(s => (int)s.ShiftStatusId == filter.ShiftStatusId.Value).ToList();
            }

            // Filter by staff ID
            if (filter.StaffId.HasValue)
            {
                shifts = shifts.Where(s => s.AssignedStaffId.HasValue && s.AssignedStaffId.Value == filter.StaffId.Value).ToList();
            }

            // Map to DTOs
            var dtos = shifts.Select(shift =>
            {
                var assignedStaff = shift.AssignedStaffId.HasValue
                    ? staff.FirstOrDefault(s => s.StaffId == shift.AssignedStaffId.Value)
                    : null;

                return new PlannedShiftDetailDto
                {
                    PlannedShiftId = shift.PlannedShiftId,
                    ShiftDate = shift.ShiftDate,
                    SlotNumber = shift.SlotNumber,
                    ShiftTypeId = (int)shift.ShiftTypeId,
                    DepartmentId = shift.DepartmentId,
                    ShiftStatusId = (int)shift.ShiftStatusId,
                    AssignedStaffId = shift.AssignedStaffId,

                    ShiftTypeName = shiftTypes.FirstOrDefault(st => st.ShiftTypeId == (int)shift.ShiftTypeId)?.ShiftTypeName ?? string.Empty,
                    ShiftDeparmentName = departments.FirstOrDefault(d => d.DepartmentId == shift.DepartmentId)?.DepartmentName ?? string.Empty,
                    ShiftStatusName = shiftStatuses.FirstOrDefault(ss => ss.ShiftStatusId == (int)shift.ShiftStatusId)?.ShiftStatusName ?? string.Empty,
                    AssignedStaffFullName = assignedStaff?.StaffName ?? string.Empty,
                    AssignedStaffDepartmentName = assignedStaff != null
                        ? departments.FirstOrDefault(d => d.DepartmentId == assignedStaff.StaffDepartmentId)?.DepartmentName ?? string.Empty
                        : string.Empty
                };
            }).OrderBy(s => s.ShiftDate).ToList();

            return dtos;
        }

        // Calendar UI Call
        public async Task<PlannedShiftDto?> UnassignedShiftFromStaffAsync(int plannedShiftId)
        {
            // Fetch the shift
            var shifts = await _plannedShiftRepo.GetAllAsync();
            var shift = shifts.FirstOrDefault(s => s.PlannedShiftId == plannedShiftId);

            if (shift == null)
                throw new Exception($"Planned shift with ID {plannedShiftId} not found.");

            // Unassign the staff
            shift.AssignedStaffId = null;
            shift.ShiftStatusId = ShiftStatuses.Vacant;

            _plannedShiftRepo.Update(shift); // Ensure this is an async update if supported
            await _plannedShiftRepo.SaveAsync();

            // Fetch related metadata
            var shiftType = await _shiftTypeRepo.GetByIdAsync((int)shift.ShiftTypeId);
            var department = await _departmentRepo.GetByIdAsync(shift.DepartmentId);

            // Map to DTO
            var shiftDto = new PlannedShiftDto
            {
                PlannedShiftId = shift.PlannedShiftId,
                ShiftDate = shift.ShiftDate,
                SlotNumber = shift.SlotNumber,
                ShiftTypeName = shiftType?.ShiftTypeName ?? "",
                ShiftDeparmentName = department?.DepartmentName ?? "",
                AssignedStaffFullName = "" // Shift is now unassigned
            };

            return shiftDto;
        }


        public async Task<PlannedShiftDto?> AssignedShiftToStaffAsync(int plannedShiftId, int staffId)
        {
            // Fetch the shift
            var shift = await _plannedShiftRepo.GetByIdAsync(plannedShiftId);
            if (shift == null)
                throw new Exception($"Planned shift with ID {plannedShiftId} not found.");

            // Check if the same staff is already assigned
            if (shift.AssignedStaffId == staffId)
                throw new Exception($"Staff ID {staffId} is already assigned to this shift.");

            // Optional: check if the shift is already occupied by someone else
            if (shift.AssignedStaffId.HasValue && shift.AssignedStaffId != staffId)
            {
                // You may choose to log or handle this differently
                // e.g., force override, or reject
                throw new Exception($"Shift is already assigned to another staff member (ID {shift.AssignedStaffId}).");
            }

            // Assign staff
            shift.AssignedStaffId = staffId;
            shift.ShiftStatusId = ShiftStatuses.Scheduled;

            _plannedShiftRepo.Update(shift);
            await _plannedShiftRepo.SaveAsync();

            // Fetch related metadata
            var shiftTypeTask = _shiftTypeRepo.GetByIdAsync((int)shift.ShiftTypeId);
            var departmentTask = _departmentRepo.GetByIdAsync(shift.DepartmentId);
            var staffTask = _staffRepo.GetByIdAsync(staffId);

            await Task.WhenAll(shiftTypeTask, departmentTask, staffTask);

            // Map to DTO
            var shiftDto = new PlannedShiftDto
            {
                PlannedShiftId = shift.PlannedShiftId,
                ShiftDate = shift.ShiftDate,
                SlotNumber = shift.SlotNumber,
                ShiftTypeName = shiftTypeTask.Result?.ShiftTypeName ?? "N/A",
                ShiftDeparmentName = departmentTask.Result?.DepartmentName ?? "N/A",
                AssignedStaffFullName = staffTask.Result?.StaffName ?? "Unknown"
            };

            return shiftDto;
        }
    }
}
