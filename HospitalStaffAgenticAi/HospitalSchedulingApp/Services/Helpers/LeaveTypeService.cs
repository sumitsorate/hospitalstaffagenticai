using HospitalSchedulingApp.Common.Enums;
using HospitalSchedulingApp.Dal.Entities;
using HospitalSchedulingApp.Dal.Repositories;
using HospitalSchedulingApp.Services.Interfaces;

namespace HospitalSchedulingApp.Services.Helpers
{
    public class LeaveTypeService : ILeaveTypeService
    {
        private readonly IRepository<LeaveTypes> _leaveTypeRepo;

        public LeaveTypeService(IRepository<LeaveTypes> leaveTypeRepo)
        {
            _leaveTypeRepo = leaveTypeRepo ?? throw new ArgumentNullException(nameof(leaveTypeRepo));
        }

        public async Task<LeaveTypes> FetchLeaveType(string leaveTypePart)
        {
            if (string.IsNullOrWhiteSpace(leaveTypePart))
                return null;

            var allLeaveTypes = await _leaveTypeRepo.GetAllAsync();


            return allLeaveTypes
                .FirstOrDefault(s =>
                    s.LeaveTypeName != null &&
                    s.LeaveTypeName.Contains(leaveTypePart, StringComparison.OrdinalIgnoreCase));
        }
    }

}
