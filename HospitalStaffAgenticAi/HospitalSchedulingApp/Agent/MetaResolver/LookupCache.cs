using HospitalSchedulingApp.Common.Enums;
using HospitalSchedulingApp.Dal.Entities;
using HospitalSchedulingApp.Dal.Repositories;

namespace HospitalSchedulingApp.Agent.MetaResolver
{
    public static class LookupCache
    {
        public static List<Staff> Staffs { get; private set; } = new();
        public static List<Department> Departments { get; private set; } = new();
        public static List<Role> Roles { get; private set; } = new();
        public static List<ShiftType> ShiftTypes { get; private set; } = new();
        public static List<ShiftStatus> ShiftStatuses { get; private set; } = new();
        public static List<LeaveTypes> LeaveTypes { get; private set; } = new();

        public static List<LeaveStatus> LeaveStatuses { get; private set; } = new();


        // Initialize using repositories
        public static async Task InitializeAsync(
             IRepository<Staff> staffRepo,
            IRepository<Department> departmentRepo,
            IRepository<Role> roleRepo,
            IRepository<ShiftType> shiftTypeRepo,
            IRepository<ShiftStatus> shiftStatusRepo,
            IRepository<LeaveTypes> leaveTypeRepo,
            IRepository<LeaveStatus> leaveStatusRepo)
        {
            Staffs = (await staffRepo.GetAllAsync()).ToList();
            Departments = (await departmentRepo.GetAllAsync()).ToList();
            Roles = (await roleRepo.GetAllAsync()).ToList();
            ShiftTypes = (await shiftTypeRepo.GetAllAsync()).ToList();
            ShiftStatuses = (await shiftStatusRepo.GetAllAsync()).ToList();
            LeaveTypes = (await leaveTypeRepo.GetAllAsync()).ToList();
            LeaveStatuses = (await leaveStatusRepo.GetAllAsync()).ToList();
        }
    }

}
