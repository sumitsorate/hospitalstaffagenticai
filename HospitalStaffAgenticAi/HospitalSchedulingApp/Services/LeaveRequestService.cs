using HospitalSchedulingApp.Common.Enums;
using HospitalSchedulingApp.Dal.Entities;
using HospitalSchedulingApp.Dal.Repositories;
using HospitalSchedulingApp.Services.Interfaces;

namespace HospitalSchedulingApp.Services
{
    public class LeaveRequestService : ILeaveRequestService
    {
        private readonly IRepository<LeaveRequests> _leaveRequestRepo;

        public LeaveRequestService(IRepository<LeaveRequests> leaveRequestRepo)
        {
            _leaveRequestRepo = leaveRequestRepo ?? throw new ArgumentNullException(nameof(leaveRequestRepo));
        }

        /// <summary>
        /// Submits a new leave request.
        /// </summary>
        public async Task<LeaveRequests> SubmitLeaveRequestAsync(LeaveRequests request)
        {
            await _leaveRequestRepo.AddAsync(request);
            await _leaveRequestRepo.SaveAsync();
            return request;
        }

        /// <summary>
        /// Fetches a leave request based on staffId, leaveStart, and leaveEnd.
        /// </summary>
        public async Task<LeaveRequests?> FetchLeaveRequestInfoAsync(int staffId, DateTime leaveStart, DateTime leaveEnd)
        {
            var leave = (await _leaveRequestRepo.GetAllAsync())
                .FirstOrDefault(l =>
                    l.StaffId == staffId &&
                    l.LeaveStart.Date == leaveStart.Date &&
                    l.LeaveEnd.Date == leaveEnd.Date &&
                    (l.LeaveStatusId == LeaveRequestStatuses.Pending || l.LeaveStatusId == LeaveRequestStatuses.Approved));

            return leave;
        }

        /// <summary>
        /// Cancels an existing leave request.
        /// </summary>
        public async Task<LeaveRequests> CancelLeaveRequestAsync(LeaveRequests request)
        {
            _leaveRequestRepo.Delete(request);
            await _leaveRequestRepo.SaveAsync();
            return request;
        }

        /// <summary>
        /// Checks if a leave overlaps with any existing approved or pending leave.
        /// </summary>
        public async Task<bool> CheckIfLeaveAlreadyExists(LeaveRequests request)
        {
            var existingLeaves = await _leaveRequestRepo.GetAllAsync();

            bool overlapExists = existingLeaves.Any(l =>
                l.StaffId == request.StaffId &&
                (l.LeaveStatusId == LeaveRequestStatuses.Pending || l.LeaveStatusId == LeaveRequestStatuses.Approved) &&
                l.LeaveStart <= request.LeaveEnd &&
                l.LeaveEnd >= request.LeaveStart);

            return overlapExists;
        }
    }

}
