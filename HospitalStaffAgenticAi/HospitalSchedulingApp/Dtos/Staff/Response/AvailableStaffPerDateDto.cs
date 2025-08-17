namespace HospitalSchedulingApp.Dtos.Staff.Response
{
    public class AvailableStaffPerDateDto
    {
        public DateOnly Date { get; set; }
        // public List<StaffDto> AvailableStaff { get; set; } = new();
        public List<ScoredStaffDto> AvailableStaff { get; set; } = new();

    }
}
