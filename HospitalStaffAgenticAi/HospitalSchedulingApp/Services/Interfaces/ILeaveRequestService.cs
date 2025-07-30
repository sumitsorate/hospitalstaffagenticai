using HospitalSchedulingApp.Dal.Entities;

namespace HospitalSchedulingApp.Services.Interfaces
{
    public interface ILeaveRequestService
    {
        Task<LeaveRequests> SubmitLeaveRequestAsync(LeaveRequests request);

        Task<bool> CheckIfLeaveAlreadyExists(LeaveRequests request);

        Task<LeaveRequests> CancelLeaveRequestAsync(LeaveRequests request);

        Task<LeaveRequests?> FetchLeaveRequestInfoAsync(int staffId, DateTime leaveStart, DateTime leaveEnd);
    }
}
