using HospitalSchedulingApp.Common.Enums;
using HospitalSchedulingApp.Dal.Entities;
using HospitalSchedulingApp.Dal.Repositories; 
using HospitalSchedulingApp.Services.AuthServices.Interfaces;
using HospitalSchedulingApp.Services.Interfaces; 
using System.Globalization;
using System.Text.RegularExpressions;

namespace HospitalSchedulingApp.Agent.MetaResolver
{
    public class EntityResolver : IEntityResolver
    {
        private readonly IRepository<Department> _departmentRepo;
        private readonly IRepository<ShiftType> _shiftTypeRepo;
        private readonly IRepository<ShiftStatus> _shiftStatusRepo;
        private readonly IRepository<Staff> _staffRepo;
        private readonly IRepository<LeaveStatus> _leaveStatusRepo;
        private readonly IRepository<LeaveTypes> _leaveTypesRepo;
        private readonly IRepository<Role> _roleRepo;

        private readonly IDepartmentService _departmentService;
        private readonly IStaffService _staffService;
        private readonly IUserContextService _userContextService;

        private readonly ILogger<EntityResolver> _logger;

        #region Synonyms
        private static readonly Dictionary<string, LeaveType> LeaveTypeSynonyms = new(StringComparer.OrdinalIgnoreCase)
        {
            { "sick", LeaveType.Sick }, { "illness", LeaveType.Sick }, { "medical", LeaveType.Sick },
            { "casual", LeaveType.Casual }, { "personal", LeaveType.Casual }, { "urgent", LeaveType.Casual },
            { "vacation", LeaveType.Vacation }, { "holiday", LeaveType.Vacation }, { "annual", LeaveType.Vacation },
            { "earned", LeaveType.Vacation }, { "leave", LeaveType.Vacation }
        };

        private static readonly Dictionary<string, string> LeaveStatusSynonyms = new(StringComparer.OrdinalIgnoreCase)
        {
            { "pending", "Pending" }, { "in progress", "Pending" }, { "awaiting", "Pending" },
            { "waiting", "Pending" }, { "approved", "Approved" }, { "approve", "Approved" },
            { "accept", "Approved" }, { "accepted", "Approved" }, { "granted", "Approved" },
            { "rejected", "Rejected" }, { "reject", "Rejected" }, { "deny", "Rejected" },
            { "denied", "Rejected" }, { "refused", "Rejected" }
        };

        private static readonly Dictionary<string, string> ShiftTypeSynonyms = new(StringComparer.OrdinalIgnoreCase)
        {
            { "night", "Night" }, { "n", "Night" }, { "morning", "Morning" },
            { "m", "Morning" }, { "day", "Morning" }, { "evening", "Evening" },
            { "e", "Evening" }
        };

        private static readonly Dictionary<string, string> ShiftStatusSynonyms = new(StringComparer.OrdinalIgnoreCase)
        {
            { "scheduled", "Scheduled" }, { "plan", "Scheduled" }, { "planned", "Scheduled" },
            { "assigned", "Assigned" }, { "assigned to", "Assigned" }, { "completed", "Completed" },
            { "done", "Completed" }, { "finished", "Completed" }, { "cancelled", "Cancelled" },
            { "canceled", "Cancelled" }, { "aborted", "Cancelled" }, { "vacant", "Vacant" },
            { "open", "Vacant" }, { "unassigned", "Vacant" }
        };
        #endregion

        public EntityResolver(
            IRepository<Department> departmentRepo,
            IRepository<ShiftType> shiftTypeRepo,
            IRepository<ShiftStatus> shiftStatusRepo,
            IRepository<Staff> staffRepo,
            IDepartmentService departmentService,
            IStaffService staffService,
            IRepository<LeaveStatus> leaveStatusRepo,
            IRepository<LeaveTypes> leaveTypesRepo,
            IRepository<Role> roleRepo,
            IUserContextService userContextService,
            ILogger<EntityResolver> logger)
        {
            _departmentRepo = departmentRepo;
            _shiftTypeRepo = shiftTypeRepo;
            _shiftStatusRepo = shiftStatusRepo;
            _staffRepo = staffRepo;
            _departmentService = departmentService;
            _staffService = staffService;
            _leaveStatusRepo = leaveStatusRepo;
            _leaveTypesRepo = leaveTypesRepo;
            _roleRepo = roleRepo;
            _userContextService = userContextService;
            _logger = logger;
        }

        public async Task<ResolveEntitiesResult> ResolveEntitiesAsync(string phrase)
        {
            if (string.IsNullOrWhiteSpace(phrase))
                return new ResolveEntitiesResult();

            _logger.LogInformation("Resolving entities for: {Phrase}", phrase);

            var tokens = phrase.Trim().ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
             

            var department = await ResolveDepartmentAsync(phrase); 
            var shiftType = await ResolveShiftTypeAsync(tokens);
            var shiftStatus = await ResolveShiftStatusAsync(tokens);
            var leaveStatus = await ResolveLeaveStatusAsync(tokens, phrase);
            var leaveType = await ResolveLeaveTypeAsync(tokens, phrase);
            var role = await ResolveUserRoleAsync();

            
            return new ResolveEntitiesResult
            {
                Department = department,              
                ShiftType = shiftType,
                ShiftStatus = shiftStatus,
                LeaveStatus = leaveStatus,
                LeaveType = leaveType,
                LoggedInUserRole = role,
                
            };
        }

        #region Resolution Methods

 
        private async Task<Department?> ResolveDepartmentAsync(string phrase) =>
            await _departmentService.FetchDepartmentInformationAsync(phrase.ToLower());

        private async Task<Staff?> ResolveStaffAsync(string phrase)
        {
            var matches = await _staffService.FetchActiveStaffByNamePatternAsync(phrase.ToLower());
            return matches?.FirstOrDefault() is var staff && staff != null
                ? new Staff
                {
                    StaffId = staff.StaffId,
                    StaffDepartmentId = staff.StaffDepartmentId,
                    IsActive = staff.IsActive,
                    RoleId = staff.RoleId,
                    StaffName = staff.StaffName
                }
                : null;
        }

        private async Task<ShiftType?> ResolveShiftTypeAsync(string[] tokens)
        {
            var shiftTypes = await _shiftTypeRepo.GetAllAsync();
            foreach (var token in tokens)
                if (ShiftTypeSynonyms.TryGetValue(token, out var canonical))
                    return shiftTypes.FirstOrDefault(st => st.ShiftTypeName.Equals(canonical, StringComparison.OrdinalIgnoreCase));

            return null;
        }

        private async Task<ShiftStatus?> ResolveShiftStatusAsync(string[] tokens)
        {
            var shiftStatuses = await _shiftStatusRepo.GetAllAsync();
            foreach (var token in tokens)
                if (ShiftStatusSynonyms.TryGetValue(token, out var canonical))
                    return shiftStatuses.FirstOrDefault(ss => ss.ShiftStatusName.Equals(canonical, StringComparison.OrdinalIgnoreCase));

            return null;
        }

        private async Task<LeaveStatus?> ResolveLeaveStatusAsync(string[] tokens, string phrase)
        {
            var leaveStatuses = await _leaveStatusRepo.GetAllAsync();
            foreach (var kvp in LeaveStatusSynonyms)
                if (phrase.ToLower().Contains(kvp.Key))
                    return leaveStatuses.FirstOrDefault(ls => ls.LeaveStatusName.Equals(kvp.Value, StringComparison.OrdinalIgnoreCase));

            foreach (var token in tokens)
                if (LeaveStatusSynonyms.TryGetValue(token, out var status))
                    return leaveStatuses.FirstOrDefault(ls => ls.LeaveStatusName.Equals(status, StringComparison.OrdinalIgnoreCase));

            return null;
        }

        private async Task<LeaveTypes?> ResolveLeaveTypeAsync(string[] tokens, string phrase)
        {
            var leaveTypes = await _leaveTypesRepo.GetAllAsync();
            foreach (var kvp in LeaveTypeSynonyms)
                if (phrase.ToLower().Contains(kvp.Key))
                    return leaveTypes.FirstOrDefault(lt => lt.LeaveTypeName.Equals(kvp.Value.ToString(), StringComparison.OrdinalIgnoreCase));

            foreach (var token in tokens)
                if (LeaveTypeSynonyms.TryGetValue(token, out var type))
                    return leaveTypes.FirstOrDefault(lt => lt.LeaveTypeName.Equals(type.ToString(), StringComparison.OrdinalIgnoreCase));

            return null;
        }

        private async Task<Role?> ResolveUserRoleAsync()
        {
            var roleName = _userContextService.GetRole();
            if (string.IsNullOrEmpty(roleName)) return null;

            var roles = await _roleRepo.GetAllAsync();
            return roles.FirstOrDefault(r => r.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase));
        }

        #endregion
    }
}
