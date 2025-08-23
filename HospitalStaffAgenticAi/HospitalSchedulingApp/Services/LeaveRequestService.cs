using HospitalSchedulingApp.Common.Enums;
using HospitalSchedulingApp.Common.Exceptions;
using HospitalSchedulingApp.Dal.Entities;
using HospitalSchedulingApp.Dal.Repositories;
using HospitalSchedulingApp.Dtos.LeaveRequest.Request;
using HospitalSchedulingApp.Dtos.LeaveRequest.Response;
using HospitalSchedulingApp.Services.AuthServices.Interfaces;
using HospitalSchedulingApp.Services.Interfaces;

namespace HospitalSchedulingApp.Services
{
    public class LeaveRequestService : ILeaveRequestService
    {
        private readonly IUserContextService _userContextService;
        private readonly IRepository<LeaveRequests> _leaveRequestRepo;
        private readonly IRepository<Staff> _staffRepository;
        private readonly IRepository<LeaveTypes> _leaveTypeRepository;
        private readonly IRepository<Department> _deptRepo;
        private readonly IRepository<LeaveStatus> _leaveStatusRepo;

        public LeaveRequestService(IRepository<LeaveRequests> leaveRequestRepo,
            IRepository<Staff> staffRepository,
            IRepository<LeaveTypes> leaveTypeRepository,
            IRepository<Department> deptRepo,
            IRepository<LeaveStatus> leaveStatusRepo,
            IUserContextService userContextService)
        {
            _leaveRequestRepo = leaveRequestRepo ?? throw new ArgumentNullException(nameof(leaveRequestRepo));
            _staffRepository = staffRepository ?? throw new ArgumentNullException();
            _leaveTypeRepository = leaveTypeRepository ?? throw new ArgumentNullException();
            _deptRepo = deptRepo ?? throw new ArgumentNullException();
            _leaveStatusRepo = leaveStatusRepo ?? throw new ArgumentNullException();
            _userContextService = userContextService ?? throw new ArgumentNullException(nameof(userContextService));
        }

        /// <summary>
        /// Fetches leave requests based on the given filter criteria. 
        /// </summary>
        /// <param name="filter">
        /// The <see cref="LeaveRequestFilter"/> containing filter criteria such as staff, type, status, and date ranges.
        /// </param>
        /// <returns>
        /// A list of <see cref="LeaveRequestDetailsDto"/> objects that match the specified filter criteria.
        /// </returns> 
        public async Task<List<LeaveRequestDetailsDto>> FetchLeaveRequestsAsync(LeaveRequestFilter filter)
        {
            // 🔒 Permission check
            var isEmployee = _userContextService.IsEmployee();
            var loggedInUserStaffId = _userContextService.GetStaffId();

            if (isEmployee)
            {
                // Employees can only fetch their own requests
                if (filter.StaffId.HasValue && filter.StaffId != loggedInUserStaffId)
                {
                    throw new BusinessRuleException("🚫 You're only allowed to view your own leave requests.");
                }

                // Always enforce self for employees
                filter.StaffId = loggedInUserStaffId;
            }

            // Validation: prevent duplicate leave requests for same employee/date
            var leaveRequest = await FetchLeaveRequestInfoAsync(
                staffId: filter.StaffId ?? 0,
                leaveStart: filter.StartDate.Value,
                leaveEnd: filter.EndDate.Value);

            if (leaveRequest != null)
            {
                throw new BusinessRuleException("Leave already exists for the same date for this employee.");
            }

            // Fetch related entities
            var leaveRequests = await _leaveRequestRepo.GetAllAsync();
            var allStaff = await _staffRepository.GetAllAsync();
            var leaveTypes = await _leaveTypeRepository.GetAllAsync();
            var departments = await _deptRepo.GetAllAsync();
            var leaveStatuses = await _leaveStatusRepo.GetAllAsync();

            // Build query with filters
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

            // Join with related tables
            var result = query
                .Join(allStaff,
                      lr => lr.StaffId,
                      s => s.StaffId,
                      (lr, s) => new { lr, s })
                .Join(departments,
                      temp => temp.s.StaffDepartmentId,
                      dept => dept.DepartmentId,
                      (temp, dept) => new { temp.lr, temp.s, department = dept })
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
        /// Submits a new leave request for an employee.
        /// </summary>
        /// <param name="request">
        /// The <see cref="LeaveRequests"/> entity containing the staff ID, start date, end date, and other request details.
        /// </param>
        public async Task<LeaveRequests> SubmitLeaveRequestAsync(LeaveRequests request)
        {
            // 🔒 Permission check
            if (_userContextService.IsEmployee() && request.StaffId != _userContextService.GetStaffId())
            {
                throw new BusinessRuleException(
                    "🚫 You can only submit leave requests for yourself. " +
                    "If you're trying to request leave for someone else, please contact a Scheduler.");
            }
            // 🔎 Validation: prevent duplicate leave requests for same employee/date
            var existingRequest = await FetchLeaveRequestInfoAsync(
                staffId: request.StaffId,
                leaveStart: request.LeaveStart,
                leaveEnd: request.LeaveEnd);

            if (existingRequest != null)
            {
                throw new BusinessRuleException("Leave already exists for the same date for this employee.");
            }

            // Default status = Pending
            request.LeaveStatusId = LeaveRequestStatuses.Pending;

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
            // 🔒 Permission check
            var isEmployee = _userContextService.IsEmployee();
            var loggedInUserStaffId = _userContextService.GetStaffId();

            if (isEmployee && request.StaffId != loggedInUserStaffId)
            {
                throw new BusinessRuleException("❌ You are not authorized to cancel this leave request.");

            }
            // 🔎 Validation: prevent duplicate leave requests for same employee/date
            var existingRequest = await FetchLeaveRequestInfoAsync(
                staffId: request.StaffId,
                leaveStart: request.LeaveStart,
                leaveEnd: request.LeaveEnd);

            if (existingRequest != null)
            {
                throw new BusinessRuleException("Leave already exists for the same date for this employee.");
            }


            _leaveRequestRepo.Delete(request);
            await _leaveRequestRepo.SaveAsync();
            return request;
        }

        /// <summary>
        /// Updates the status of a leave request.
        /// </summary>
        /// <param name="leaveRequestId">
        /// The unique identifier of the leave request to update.
        /// </param>
        /// <param name="newStatus">
        /// The new <see cref="LeaveRequestStatuses"/> value to assign.
        /// </param>
        /// <returns>
        public async Task<LeaveRequestDetailsDto?> UpdateStatusAsync(int leaveRequestId, LeaveRequestStatuses newStatus)
        {
            if (!_userContextService.IsScheduler())
            {
                throw new BusinessRuleException( "🚫 Oops! You're not authorized to perform this action.");
            }
            var leaveRequest = await _leaveRequestRepo.GetByIdAsync(leaveRequestId);

            if (leaveRequest == null)
                throw new BusinessRuleException("Leave request not found.");

            if (leaveRequest.LeaveStatusId != LeaveRequestStatuses.Pending)
                throw new BusinessRuleException("Only pending leave requests can be updated.");

            if (leaveRequest.LeaveStatusId == newStatus)
                throw new BusinessRuleException("The current status and the target status cannot be the same.");

            // ✅ Update status
            leaveRequest.LeaveStatusId = newStatus;
            _leaveRequestRepo.Update(leaveRequest);
            await _leaveRequestRepo.SaveAsync();

            // 🔄 Reload with related details (joins staff, type, etc.)
            var leaveRequestDetails = await FetchLeaveRequestsAsync(new LeaveRequestFilter
            {
                LeaveRequestId = leaveRequestId
            });

            return leaveRequestDetails.FirstOrDefault();
        }
    }

}
