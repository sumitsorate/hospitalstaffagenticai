using HospitalSchedulingApp.Common.Enums;
using HospitalSchedulingApp.Dal.Entities;
using HospitalSchedulingApp.Dal.Repositories;
using HospitalSchedulingApp.Dtos.Staff;
using HospitalSchedulingApp.Dtos.Staff.Requests;
using HospitalSchedulingApp.Dtos.Staff.Response;
using HospitalSchedulingApp.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Data;

namespace HospitalSchedulingApp.Services
{
    /// <summary>
    /// Service responsible for staff-related operations including availability and filtering.
    /// </summary>
    public class StaffService : IStaffService
    {
        private readonly IRepository<Department> _departmentRepo;
        private readonly IRepository<Role> _roleRepo;
        private readonly IRepository<Staff> _staffRepo;
        private readonly IRepository<PlannedShift> _shiftRepo;
        private readonly IRepository<LeaveRequests> _leaveRepo;
        private readonly IRepository<NurseAvailability> _availabilityRepo;
        private readonly IShiftTypeService _shiftTypeService;
        private readonly ILogger<StaffService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="StaffService"/> class.
        /// </summary>
        public StaffService(
            IRepository<Department> departmentRepo,
            IRepository<Role> roleRepo,
            IRepository<Staff> staffRepo,
            IRepository<PlannedShift> shiftRepo,
            IRepository<LeaveRequests> leaveRepo,
            IRepository<NurseAvailability> availabilityRepo,
            IRepository<ShiftType> shiftTypeRepo,
            IShiftTypeService shiftTypeService,
            ILogger<StaffService> logger)
        {
            _departmentRepo = departmentRepo;
            _roleRepo = roleRepo;
            _staffRepo = staffRepo;
            _shiftRepo = shiftRepo;
            _leaveRepo = leaveRepo;
            _availabilityRepo = availabilityRepo;
            _shiftTypeService = shiftTypeService;
            _logger = logger;
        }

        /// <summary>
        /// Fetches a list of active staff whose names match the provided name pattern.
        /// </summary>
        /// <param name="namePart">Partial or full name to search for.</param>
        /// <returns>List of matching active staff.</returns>
        public async Task<List<StaffDto?>> FetchActiveStaffByNamePatternAsync(string namePart)
        {
            if (string.IsNullOrWhiteSpace(namePart))
                return new List<StaffDto?>();

            var staffList = await _staffRepo.GetAllAsync();
            var roles = await _roleRepo.GetAllAsync();
            var departments = await _departmentRepo.GetAllAsync();

            var filtered = staffList
                .Where(s => s.IsActive && s.StaffName.Contains(namePart, StringComparison.OrdinalIgnoreCase))
                .OrderBy(s => s.StaffName)
                .Take(10)
                .Select(s => new StaffDto
                {
                    StaffId = s.StaffId,
                    StaffName = s.StaffName,
                    RoleId = s.RoleId,
                    RoleName = roles.FirstOrDefault(r => r.RoleId == s.RoleId)?.RoleName ?? string.Empty,
                    StaffDepartmentId = s.StaffDepartmentId,
                    StaffDepartmentName = departments.FirstOrDefault(d => d.DepartmentId == s.StaffDepartmentId)?.DepartmentName ?? string.Empty
                })
                .ToList();

            _logger.LogInformation("Fetched {Count} staff matching name pattern: {Pattern}", filtered.Count, namePart);
            return filtered;
        }

        public async Task<List<AvailableStaffPerDateDto?>> SearchAvailableStaffAsync(AvailableStaffFilterDto filter)
        {
            var staffList = await _staffRepo.GetAllAsync();
            var shifts = await _shiftRepo.GetAllAsync();
            var leaveRequests = await _leaveRepo.GetAllAsync();
            var availabilities = await _availabilityRepo.GetAllAsync();
            var departments = await _departmentRepo.GetAllAsync();

            var departmentMap = departments.ToDictionary(d => d.DepartmentId, d => d.DepartmentName);

            var dateRange = Enumerable.Range(0, (filter.EndDate.DayNumber - filter.StartDate.DayNumber + 1))
                                      .Select(offset => filter.StartDate.AddDays(offset))
                                      .ToList();

            var result = new List<AvailableStaffPerDateDto>();

            foreach (var date in dateRange)
            {
                var shiftDate = date.ToDateTime(TimeOnly.MinValue);
                var previousDate = shiftDate.AddDays(-1);

                var availableStaffForDate = staffList
                    .Where(s => s.IsActive)
                    .Where(s =>
                    {
                        if (filter.DepartmentId.HasValue && s.StaffDepartmentId != filter.DepartmentId.Value)
                            return false;

                        var isAvailable = !availabilities.Any(a =>
                            a.StaffId == s.StaffId &&
                            a.AvailableDate == shiftDate &&
                            !a.IsAvailable);

                        var isOnLeave = leaveRequests.Any(l =>
                            l.StaffId == s.StaffId &&
                            l.LeaveStatusId == LeaveRequestStatuses.Approved &&
                            shiftDate >= l.LeaveStart &&
                            shiftDate <= l.LeaveEnd);

                        if (!isAvailable || isOnLeave)
                            return false;

                        // Get shifts of this staff
                        var todaysShifts = shifts.Where(ps => ps.AssignedStaffId == s.StaffId && ps.ShiftDate == shiftDate).ToList();
                        var yesterdaysShifts = shifts.Where(ps => ps.AssignedStaffId == s.StaffId && ps.ShiftDate == previousDate).ToList();

                        if (filter.ShiftTypeId.HasValue)
                        {
                            var shiftType = (ShiftTypes)filter.ShiftTypeId.Value;
                            if (filter.ApplyFatigueCheck)
                            {
                                // Check same-day conflict
                                if (todaysShifts.Any(ps =>
                                ps.ShiftTypeId == shiftType || // Same shift
                                IsBackToBack(ps.ShiftTypeId, shiftType))) // Fatigue risk
                                {
                                    return false;
                                }

                                // Special check for Morning shift after previous day's Night
                                if (shiftType == ShiftTypes.Morning &&
                                    yesterdaysShifts.Any(ps => ps.ShiftTypeId == ShiftTypes.Night))
                                {
                                    return false;
                                }
                            }
                        }

                        return true;
                    })
                    .Select(s =>
                    {
                        departmentMap.TryGetValue(s.StaffDepartmentId, out var deptName);

                        

                        return new StaffDto
                        {
                            StaffId = s.StaffId,
                            StaffName = s.StaffName,
                            RoleId = s.RoleId, 
                            StaffDepartmentId = s.StaffDepartmentId,
                            StaffDepartmentName = deptName ?? string.Empty,
                            IsActive = s.IsActive
                        };
                    })
                    .ToList();

                result.Add(new AvailableStaffPerDateDto
                {
                    Date = date,
                    AvailableStaff = availableStaffForDate
                });

                _logger.LogDebug("Date: {Date}, Available staff count: {Count}", date, availableStaffForDate.Count);
            }

            _logger.LogInformation("Completed search for available staff from {Start} to {End}", filter.StartDate, filter.EndDate);
            return result;
        }

        /// <summary>
        /// Determines if two shifts are adjacent (fatigue risk).
        /// </summary>
        private bool IsBackToBack(ShiftTypes assigned, ShiftTypes candidate)
        {
            return (assigned == ShiftTypes.Morning && candidate == ShiftTypes.Evening)
                || (assigned == ShiftTypes.Evening && candidate == ShiftTypes.Night)
                || (assigned == ShiftTypes.Night && candidate == ShiftTypes.Morning);
        }

    }
}
