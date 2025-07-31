using HospitalSchedulingApp.Common.Enums;
using HospitalSchedulingApp.Dal.Entities;
using HospitalSchedulingApp.Dal.Repositories;
using HospitalSchedulingApp.Dtos.LeaveRequest.Request;
using HospitalSchedulingApp.Dtos.LeaveRequest.Response;
using HospitalSchedulingApp.Services.Interfaces;

namespace HospitalSchedulingApp.Services
{
    public class LeaveRequestService : ILeaveRequestService
    {
        private readonly IRepository<LeaveRequests> _leaveRequestRepo;
        private readonly IRepository<Staff> _staffRepository;
        private readonly IRepository<LeaveTypes> _leaveTypeRepository;
        private readonly IRepository<Department> _deptRepo;
        private readonly IRepository<LeaveStatus> _leaveStatusRepo;

        public LeaveRequestService(IRepository<LeaveRequests> leaveRequestRepo,
            IRepository<Staff> staffRepository,
            IRepository<LeaveTypes> leaveTypeRepository,
            IRepository<Department> deptRepo,
             IRepository<LeaveStatus> leaveStatusRepo)
        {
            _leaveRequestRepo = leaveRequestRepo ?? throw new ArgumentNullException(nameof(leaveRequestRepo));
            _staffRepository = staffRepository ?? throw new ArgumentNullException();
            _leaveTypeRepository = leaveTypeRepository ?? throw new ArgumentNullException();
            _deptRepo = deptRepo ?? throw new ArgumentNullException();
            _leaveStatusRepo = leaveStatusRepo ?? throw new ArgumentNullException();
        }

        /// <summary>
        /// Submits a new leave request.
        /// </summary>
        public async Task<LeaveRequests> SubmitLeaveRequestAsync(LeaveRequests request)
        {
            request.LeaveStatusId = LeaveRequestStatuses.Pending;
            await _leaveRequestRepo.AddAsync(request);
            await _leaveRequestRepo.SaveAsync();
            return request;
        }

        /// <summary>
        /// Fetch Leave Requests by Leave Request filter
        /// </summary>
        /// <summary>
        /// Fetch leave requests using all applicable filter criteria.
        /// </summary>
        public async Task<List<LeaveRequestDetailsDto>> FetchLeaveRequestsAsync(LeaveRequestFilter filter)
        {
            var leaveRequests = await _leaveRequestRepo.GetAllAsync();
            var allStaff = await _staffRepository.GetAllAsync();
            var leaveTypes = await _leaveTypeRepository.GetAllAsync();
            var departments = await _deptRepo.GetAllAsync();
            var leaveStatuses = await _leaveStatusRepo.GetAllAsync();

            var query = leaveRequests.AsQueryable();

            if (filter.LeaveRequestId.HasValue)
                query = query.Where(lr => lr.Id == filter.LeaveRequestId.Value);

            if (filter.StaffId.HasValue)
                query = query.Where(lr => lr.StaffId == filter.StaffId.Value);

            if (filter.LeaveStatusId.HasValue)
                query = query.Where(lr => lr.LeaveStatusId == filter.LeaveStatusId.Value);

            if (filter.LeaveTypeId.HasValue)
                query = query.Where(lr => lr.LeaveTypeId == filter.LeaveTypeId.Value);

            if (filter.StartDate.HasValue)
                query = query.Where(lr => lr.LeaveEnd.Date >= filter.StartDate.Value.Date);

            if (filter.EndDate.HasValue)
                query = query.Where(lr => lr.LeaveStart.Date <= filter.EndDate.Value.Date);

            var result = query
                .Join(allStaff,
                      lr => lr.StaffId,
                      s => s.StaffId,
                      (lr, s) => new { lr, s })
                .Join(departments,
                      temp => temp.s.StaffDepartmentId,
                      dept => dept.DepartmentId,
                      (temp, dept) => new { lr = temp.lr, s = temp.s, department = dept })
                .Join(leaveTypes,
                      temp => (int)temp.lr.LeaveTypeId,
                      leaveType => leaveType.LeaveTypeId,
                      (temp, leaveType) => new { temp.lr, temp.s, temp.department, leaveType })
                .Join(leaveStatuses,
                      temp => (int)temp.lr.LeaveStatusId,
                      leaveStatus => leaveStatus.LeaveStatusId,
                      (temp, leaveStatus) => new LeaveRequestDetailsDto
                      {
                          LeaveRequestId = temp.lr.Id,
                          StaffId = temp.lr.StaffId,
                          StaffName = temp.s.StaffName,
                          StaffDepartmentId = temp.department.DepartmentId,
                          StaffDepartmentName = temp.department.DepartmentName,
                          LeaveStart = temp.lr.LeaveStart,
                          LeaveEnd = temp.lr.LeaveEnd,
                          LeaveStatus = temp.lr.LeaveStatusId,
                          LeaveStatusName = leaveStatus.LeaveStatusName,
                          LeaveTypeId = temp.lr.LeaveTypeId,
                          LeaveTypeName = temp.leaveType.LeaveTypeName
                      })
                .OrderByDescending(x => x.LeaveStart)
                .ToList();

            return result;
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

        public async Task<LeaveRequestDetailsDto?> UpdateStatusAsync(int leaveRequestId, LeaveRequestStatuses newStatus)
        {
            var leaveRequest = await _leaveRequestRepo.GetByIdAsync(leaveRequestId);
            if (leaveRequest == null)
                return null; // Not found

            if (leaveRequest.LeaveStatusId != LeaveRequestStatuses.Pending)
                return null; // Can only update pending requests

            if (leaveRequest.LeaveStatusId == newStatus)
                return null;   // Already same, return as-is

            // Update status
            leaveRequest.LeaveStatusId = newStatus;
            _leaveRequestRepo.Update(leaveRequest);
            await _leaveRequestRepo.SaveAsync();

            // Reload related data if needed (e.g., staff, type, etc.)
            var leaveRequestDetails = await FetchLeaveRequestsAsync(new LeaveRequestFilter
            {
                LeaveRequestId = leaveRequestId
            });
            return leaveRequestDetails.FirstOrDefault();
        }
    }

}
