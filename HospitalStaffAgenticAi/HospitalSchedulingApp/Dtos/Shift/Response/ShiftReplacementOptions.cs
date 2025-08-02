using HospitalSchedulingApp.Dtos.Staff.Response;

namespace HospitalSchedulingApp.Dtos.Shift.Response
{
    public class ShiftReplacementOptions
    {
        public int PlannedShiftId { get; set; }
        public DateTime ShiftDate { get; set; }
        public int ShiftTypeId { get; set; }
        public string ShiftTypeName { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public List<AvailableStaffPerDateDto?> ShiftReplacements { get; set; }  = null;
    }
}
