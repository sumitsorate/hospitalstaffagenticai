using HospitalSchedulingApp.Common.Enums;
using HospitalSchedulingApp.Dal.Entities;
using HospitalSchedulingApp.Dtos.LeaveRequest.Request;
using HospitalSchedulingApp.Dtos.LeaveRequest.Response;

namespace HospitalSchedulingApp.Services.Interfaces
{
    /// <summary>
    /// Defines contract for leave request management operations such as 
    /// submission, cancellation, validation, fetching, and status updates.
    /// </summary>
    public interface ILeaveRequestService
    {
        /// <summary>
        /// Submits a new leave request for staff.
        /// </summary>
        /// <param name="request">The leave request entity containing staff, type, and dates.</param>
        /// <returns>The saved <see cref="LeaveRequests"/> entity.</returns>
        Task<LeaveRequests> SubmitLeaveRequestAsync(LeaveRequests request);

        /// <summary>
        /// Cancels an existing leave request.
        /// </summary>
        /// <param name="request">The leave request entity to cancel.</param>
        /// <returns>The updated <see cref="LeaveRequests"/> entity with cancelled status.</returns>
        Task<LeaveRequests> CancelLeaveRequestAsync(LeaveRequests request);

        /// <summary>
        /// Fetches a leave request for a specific staff member and date range.
        /// </summary>
        /// <param name="staffId">The staff ID.</param>
        /// <param name="leaveStart">The start date of the leave.</param>
        /// <param name="leaveEnd">The end date of the leave.</param>
        /// <returns>The matching <see cref="LeaveRequests"/> entity if found, otherwise null.</returns>
        Task<LeaveRequests?> FetchLeaveRequestInfoAsync(int staffId, DateTime leaveStart, DateTime leaveEnd);

        /// <summary>
        /// Fetches multiple leave requests based on filter criteria.
        /// </summary>
        /// <param name="filter">The filter object specifying staff, status, dates, and type.</param>
        /// <returns>A list of <see cref="LeaveRequestDetailsDto"/> matching the filter.</returns>
        Task<List<LeaveRequestDetailsDto>> FetchLeaveRequestsAsync(LeaveRequestFilter filter);

        /// <summary>
        /// Updates the status of a leave request (e.g., approve, reject, pending).
        /// </summary>
        /// <param name="leaveRequestId">The leave request ID.</param>
        /// <param name="newStatus">The new status to apply.</param>
        /// <returns>The updated <see cref="LeaveRequestDetailsDto"/> if found, otherwise null.</returns>
        Task<LeaveRequestDetailsDto?> UpdateStatusAsync(int leaveRequestId, LeaveRequestStatuses newStatus);
    }
}
