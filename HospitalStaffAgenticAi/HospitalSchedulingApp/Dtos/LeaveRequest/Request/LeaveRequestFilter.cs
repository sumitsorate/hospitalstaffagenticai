using HospitalSchedulingApp.Common.Enums;

namespace HospitalSchedulingApp.Dtos.LeaveRequest.Request
{
    public class LeaveRequestFilter
    {
        public int? StaffId { get; set; }
        public LeaveRequestStatuses? LeaveStatusId { get; set; } // "Pending", "Approved", "Rejected"
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public LeaveType? LeaveTypeId { get; set; }
    }

}
