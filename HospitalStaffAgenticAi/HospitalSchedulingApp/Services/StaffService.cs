using HospitalSchedulingApp.Common.Enums;
using HospitalSchedulingApp.Dal.Entities;
using HospitalSchedulingApp.Dal.Repositories;
using HospitalSchedulingApp.Dtos.Staff;
using HospitalSchedulingApp.Dtos.Staff.Requests;
using HospitalSchedulingApp.Dtos.Staff.Response;
using HospitalSchedulingApp.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace HospitalSchedulingApp.Services
{
    /// <summary>
    /// Service responsible for staff-related operations such as
    /// searching availability, applying fatigue rules, and filtering staff.
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
        /// Fetches up to 10 active staff whose names match the provided name pattern.
        /// </summary>
        /// <param name="namePart">Partial or full name to search for.</param>
        /// <returns>A list of <see cref="StaffDto"/> representing matching staff.</returns>
        public async Task<List<StaffDto?>> FetchActiveStaffByNamePatternAsync(string namePart)
        {
            _logger.LogInformation("Searching for active staff by name pattern: {Pattern}", namePart);

            if (string.IsNullOrWhiteSpace(namePart))
            {
                _logger.LogWarning("Search aborted: name pattern is null or empty.");
                return new List<StaffDto?>();
            }

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
                    StaffDepartmentName = departments.FirstOrDefault(d => d.DepartmentId == s.StaffDepartmentId)?.DepartmentName ?? string.Empty,
                    IsActive = s.IsActive
                })
                .ToList();

            _logger.LogInformation("Found {Count} staff matching pattern: {Pattern}", filtered.Count, namePart);

            return filtered;
        }

        /// <summary>
        /// Searches for available staff over a given date range based on filters such as department,
        /// shift type, fatigue rules, and leave/availability constraints.
        /// </summary>
        /// <param name="filter">The filter criteria including date range and shift rules.</param>
        /// <returns>A list of <see cref="AvailableStaffPerDateDto"/> objects grouped by date.</returns>
        public async Task<List<AvailableStaffPerDateDto?>> SearchAvailableStaffAsync(AvailableStaffFilterDto filter)
        {
            _logger.LogInformation("Starting search for available staff from {Start} to {End} with department {Dept} and shift {ShiftType}",
                filter.StartDate, filter.EndDate, filter.DepartmentId, filter.ShiftTypeId);

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

                _logger.LogDebug("Processing availability for date {Date}", date);

                // STEP 1️⃣ Same department, fatigue check ON
                var primaryStaff = FilterStaff(
                    staffList.ToList(),
                    shiftDate,
                    filter,
                    shifts.ToList(),
                    leaveRequests.ToList(),
                    availabilities.ToList(),
                    onlySameDepartment: true,
                    applyFatigue: true
                );

                if (!primaryStaff.Any())
                {
                    _logger.LogDebug("No staff found in same department on {Date}, retrying other departments.", date);

                    // STEP 2️⃣ Other departments, fatigue check ON
                    primaryStaff = FilterStaff(
                        staffList.ToList(),
                        shiftDate,
                        filter,
                        shifts.ToList(),
                        leaveRequests.ToList(),
                        availabilities.ToList(),
                        onlySameDepartment: false,
                        applyFatigue: true
                    );
                }

                // STEP 3️⃣ Retry with fatigue OFF if still empty and allowed
                if (!primaryStaff.Any() && !filter.ApplyFatigueCheck)
                {
                    _logger.LogDebug("Relaxing fatigue rules for {Date}.", date);

                    var relaxedSame = FilterStaff(
                        staffList.ToList(),
                        shiftDate,
                        filter,
                        shifts.ToList(),
                        leaveRequests.ToList(),
                        availabilities.ToList(),
                        onlySameDepartment: true,
                        applyFatigue: false
                    );

                    var relaxedOther = FilterStaff(
                        staffList.ToList(),
                        shiftDate,
                        filter,
                        shifts.ToList(),
                        leaveRequests.ToList(),
                        availabilities.ToList(),
                        onlySameDepartment: false,
                        applyFatigue: false
                    );

                    primaryStaff = relaxedSame.Concat(relaxedOther).ToList();
                    primaryStaff = primaryStaff
                        .GroupBy(s => s.StaffId)
                        .Select(g => g.First())
                        .ToList();

                }

                // Build result
                result.Add(new AvailableStaffPerDateDto
                {
                    Date = date,
                    AvailableStaff = primaryStaff.Select(s => new ScoredStaffDto
                    {
                        StaffId = s.StaffId,
                        StaffName = s.StaffName,
                        RoleId = s.RoleId,
                        StaffDepartmentId = s.StaffDepartmentId,
                        StaffDepartmentName = departmentMap.GetValueOrDefault(s.StaffDepartmentId, string.Empty),
                        IsActive = s.IsActive,
                        // Optional: include score/reasoning in your DTO
                        Score = s.Score,
                        Reasoning = s.Reasoning,
                        IsFatigueRisk = s.IsFatigueRisk,
                        IsCrossDepartment = s.IsCrossDepartment
                    }).ToList()
                });

                _logger.LogDebug("Date: {Date}, available staff count: {Count}", date, primaryStaff.Count);
            }

            _logger.LogInformation("Completed search for available staff from {Start} to {End}", filter.StartDate, filter.EndDate);
            return result;
        }

        /// <summary>
        /// Maps a Staff entity into a ScoredStaffDto with scoring based on department, fatigue, and availability.
        /// </summary>
        /// <param name="staff">The staff being evaluated.</param>
        /// <param name="shiftDate">The shift date being considered for assignment.</param>
        /// <param name="sameDepartment">Indicates if staff belongs to the same department.</param>
        /// <param name="fatigueApplied">Indicates if fatigue rules should be enforced.</param>
        /// <param name="allShifts">All planned shifts (used to check past and future assignments).</param>
        /// <returns>A ScoredStaffDto with computed score and reasoning.</returns>


        /// <summary>
        /// Determines if assigning a candidate shift type after an assigned shift type
        /// constitutes a back-to-back fatigue risk.
        /// </summary>
        /// <param name="assigned">The already assigned shift type.</param>
        /// <param name="candidate">The candidate shift type.</param>
        /// <returns><c>true</c> if the shifts are back-to-back; otherwise, <c>false</c>.</returns>
        private bool IsBackToBack(ShiftTypes assigned, ShiftTypes candidate)
        {
            return (assigned == ShiftTypes.Morning && candidate == ShiftTypes.Evening)
                || (assigned == ShiftTypes.Evening && candidate == ShiftTypes.Night)
                || (assigned == ShiftTypes.Night && candidate == ShiftTypes.Morning);
        }

        /// <summary>
        /// Filters staff based on department, availability, leave requests, shift assignments,
        /// and optional fatigue rules.
        /// </summary>
        /// <summary>
        /// Filters staff based on department, availability, leave requests, shift assignments,
        /// back-to-back shifts, and optional fatigue rules.
        /// </summary>
        private List<ScoredStaffDto> FilterStaff(
            List<Staff> allStaff,
            DateTime shiftDate,
            AvailableStaffFilterDto filter,
            List<PlannedShift> allShifts,
            List<LeaveRequests> leaveRequests,
            List<NurseAvailability> availabilities,
            bool onlySameDepartment,
            bool applyFatigue)
        {
            var filtered = allStaff
                .Where(s => s.IsActive)
                .Where(s =>
                {
                    // --- Department filtering ---
                    if (onlySameDepartment && filter.DepartmentId.HasValue && s.StaffDepartmentId != filter.DepartmentId.Value)
                        return false;
                    if (!onlySameDepartment && filter.DepartmentId.HasValue && s.StaffDepartmentId == filter.DepartmentId.Value)
                        return false;

                    // --- Availability check ---
                    var isAvailable = !availabilities.Any(a =>
                        a.StaffId == s.StaffId &&
                        a.AvailableDate.Date == shiftDate.Date &&
                        !a.IsAvailable);

                    // --- Leave check ---
                    var isOnLeave = leaveRequests.Any(l =>
                        l.StaffId == s.StaffId &&
                        l.LeaveStatusId == LeaveRequestStatuses.Approved &&
                        shiftDate.Date >= l.LeaveStart.Date &&
                        shiftDate.Date <= l.LeaveEnd.Date);

                    if (!isAvailable || isOnLeave)
                        return false;

                    // --- Shift type check ---
                    if (filter.ShiftTypeId.HasValue)
                    {
                        var candidateShiftType = (ShiftTypes)filter.ShiftTypeId.Value;

                        // Reject staff if already assigned the requested shift type
                        if (allShifts.Any(ps =>
                                ps.AssignedStaffId == s.StaffId &&
                                ps.ShiftDate.Date == shiftDate.Date &&
                                ps.ShiftTypeId == candidateShiftType))
                        {
                            return false;
                        }

                        if (applyFatigue)
                        {
                            // Reject staff if already assigned any shift on this date
                            if (allShifts.Any(ps => ps.AssignedStaffId == s.StaffId &&
                                                    ps.ShiftDate.Date == shiftDate.Date))
                                return false;

                            // Same-day back-to-back
                            if (allShifts.Any(ps =>
                                ps.AssignedStaffId == s.StaffId &&
                                ps.ShiftDate.Date == shiftDate.Date &&
                                IsBackToBack((ShiftTypes)ps.ShiftTypeId, candidateShiftType)))
                                return false;

                            // Previous day → Morning shift today
                            if (allShifts.Any(ps =>
                                ps.AssignedStaffId == s.StaffId &&
                                (shiftDate - ps.ShiftDate).TotalDays == 1 &&
                                candidateShiftType == ShiftTypes.Morning &&
                                ps.ShiftTypeId == ShiftTypes.Night))
                                return false;

                            // Next day → Night shift today
                            if (allShifts.Any(ps =>
                                ps.AssignedStaffId == s.StaffId &&
                                (ps.ShiftDate - shiftDate).TotalDays == 1 &&
                                candidateShiftType == ShiftTypes.Night &&
                                ps.ShiftTypeId == ShiftTypes.Evening))
                                return false;
                        }
                    }

                    return true;
                })
                .Select(s => ToScoredStaffDto(s, shiftDate, onlySameDepartment, applyFatigue, allShifts,
                filter.ShiftTypeId,filter.DepartmentId.HasValue))
                .OrderByDescending(s => s.Score)
                .ToList();

            // --- Deduplicate staff by StaffId ---
            filtered = filtered
                .GroupBy(s => s.StaffId)
                .Select(g => g.First())
                .ToList();

            return filtered;
        }


        /// <summary>
        /// Maps a Staff entity into a ScoredStaffDto with scoring based on department, fatigue, and back-to-back shift checks.
        /// </summary>
        private ScoredStaffDto ToScoredStaffDto(
            Staff staff,
            DateTime shiftDate,
            bool sameDepartment,
            bool fatigueApplied,
            List<PlannedShift> allShifts,
            int? candidateShiftTypeId,
            bool departmentFilterApplied)
        {
            double score = 0.5;
            var reasoning = new List<string>();

            if (departmentFilterApplied)
            {
                if (sameDepartment)
                {
                    score += 0.3;
                    reasoning.Add("✅ Same department match — specialist alignment improves care");
                }
                else
                {
                    reasoning.Add("ℹ️ Cross-department fallback — less optimal but still available");
                }
            }

            if (!fatigueApplied)
            {
                reasoning.Add("⚠️ Fatigue check relaxed — availability prioritized");
            }
            else
            {
                reasoning.Add("✅ Fatigue check enforced — ensuring safe scheduling");
            }

            var staffShifts = allShifts
                .Where(ps => ps.AssignedStaffId == staff.StaffId)
                .OrderBy(ps => ps.ShiftDate)
                .ToList();

            bool fatigueRisk = false;
            bool backToBackRisk = false;

            if (candidateShiftTypeId.HasValue)
            {
                var candidateShiftType = (ShiftTypes)candidateShiftTypeId.Value;

                // --- Get last shift before candidate ---
                var lastShift = staffShifts
                    .Where(ps => ps.ShiftDate <= shiftDate)
                    .OrderByDescending(ps => ps.ShiftDate)
                    .FirstOrDefault();

                if (lastShift != null)
                {
                    // --- Reward staff already in a back-to-back shift (fatigue relaxed) ---
                    if (!fatigueApplied && IsBackToBack((ShiftTypes)lastShift.ShiftTypeId, candidateShiftType))
                    {
                        reasoning.Add("✅ Already has a back-to-back shift — on-site and higher chance of accepting this shift");
                        score += 0.2; // reward for being on-site/back-to-back
                    }

                    // --- Small bonus if same-day shift but not strictly back-to-back (fatigue relaxed) ---
                    if (!fatigueApplied && lastShift.ShiftDate.Date == shiftDate.Date && !IsBackToBack((ShiftTypes)lastShift.ShiftTypeId, candidateShiftType))
                    {
                        reasoning.Add("⚠️ Already has a shift today — and may accept an additional shift");
                        score += 0.1; // smaller bonus
                    }

                    // --- Fatigue rules enforced: risk checks ---
                    if (fatigueApplied && IsBackToBack((ShiftTypes)lastShift.ShiftTypeId, candidateShiftType))
                    {
                        backToBackRisk = true;
                        reasoning.Add("⚠️ Back-to-back shift with previous shift — potential fatigue risk");
                    }

                    if (fatigueApplied && (shiftDate - lastShift.ShiftDate).TotalHours < 12)
                    {
                        fatigueRisk = true;
                        reasoning.Add("⚠️ Too close to previous shift — may cause fatigue");
                    }
                }


                // Next shift after candidate
                var nextShift = staffShifts
                    .Where(ps => ps.ShiftDate >= shiftDate)
                    .OrderBy(ps => ps.ShiftDate)
                    .FirstOrDefault();

                if (fatigueApplied && nextShift != null && (nextShift.ShiftDate - shiftDate).TotalHours < 12)
                {
                    fatigueRisk = true;
                    reasoning.Add("⚠️ Too close to upcoming shift");
                }
            }

            if (fatigueRisk) score -= 0.2;
            if (backToBackRisk) score -= 0.2;
            if (!fatigueApplied) score += 0.1; // keep your original fatigue relaxed bonus

            return new ScoredStaffDto
            {
                StaffId = staff.StaffId,
                StaffName = staff.StaffName,
                RoleId = staff.RoleId,
                StaffDepartmentId = staff.StaffDepartmentId,
                IsActive = staff.IsActive,
                Score = Math.Clamp(score, 0, 1),
                Reasoning = string.Join("; ", reasoning),
                IsFatigueRisk = fatigueRisk,
                IsBackToBackRisk = backToBackRisk,
                IsCrossDepartment = !sameDepartment
            };
        }


    }
}
