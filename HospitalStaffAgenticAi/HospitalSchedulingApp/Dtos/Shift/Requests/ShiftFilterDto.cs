namespace HospitalSchedulingApp.Dtos.Shift.Requests
{
    public class ShiftFilterDto
    {
        public int? DepartmentId { get; set; }
        public int? StaffId { get; set; }
        public int? ShiftTypeId { get; set; }
        public int? ShiftStatusId { get; set; } // <-- new
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }


}
