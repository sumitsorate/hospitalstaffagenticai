namespace HospitalSchedulingApp.Dtos.LeaveRequest
{
    public class LeaveRequestFilter
    {
        public int? StaffId { get; set; }
        public string? LeaveStatus { get; set; } // "Pending", "Approved", "Rejected"
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

}
