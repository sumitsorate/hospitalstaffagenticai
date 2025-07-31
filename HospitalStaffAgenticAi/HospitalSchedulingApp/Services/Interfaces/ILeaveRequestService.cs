using HospitalSchedulingApp.Dal.Entities;
using HospitalSchedulingApp.Dtos.LeaveRequest.Request;
using HospitalSchedulingApp.Dtos.LeaveRequest.Response;

namespace HospitalSchedulingApp.Services.Interfaces
{
    public interface ILeaveRequestService
    {
        Task<LeaveRequests> SubmitLeaveRequestAsync(LeaveRequests request);

        Task<bool> CheckIfLeaveAlreadyExists(LeaveRequests request);

        Task<LeaveRequests> CancelLeaveRequestAsync(LeaveRequests request);

        Task<LeaveRequests?> FetchLeaveRequestInfoAsync(int staffId, DateTime leaveStart, DateTime leaveEnd);

        Task<List<LeaveRequestDetailsDto>> FetchLeaveRequestsAsync(LeaveRequestFilter filter);
    }
}
