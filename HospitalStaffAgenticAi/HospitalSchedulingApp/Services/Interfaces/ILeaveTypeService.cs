using HospitalSchedulingApp.Common.Enums;
using HospitalSchedulingApp.Dal.Entities;

namespace HospitalSchedulingApp.Services.Interfaces
{
    public interface ILeaveTypeService
    {
        Task<LeaveTypes> FetchLeaveType(string leaveType);
    }
}
