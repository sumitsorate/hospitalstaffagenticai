namespace HospitalSchedulingApp.Dtos.Shift.Response
{

    public class ShiftSwapResponse
    {
        public int Id { get; set; }

        public int RequestingStaffId { get; set; }
        public string RequestingStaffName { get; set; } = string.Empty;
        public int TargetStaffId { get; set; }
        public string TargetStaffName { get; set; } = string.Empty;

        public DateTime SourceShiftDate { get; set; }
        public int SourceShiftTypeId { get; set; }
        public string SourceShiftTypeName { get; set; } = string.Empty;

        public DateTime TargetShiftDate { get; set; }
        public int TargetShiftTypeId { get; set; }
        public string TargetShiftTypeName { get; set; } = string.Empty;

        public int StatusId { get; set; }
        public string ShiftSwapStatus { get; set; } = string.Empty;

        public string SourceDepartmentName { get; set; } = string.Empty;
        public int SourceDepartmentId { get; set; }

        public string TargetDepartmentName { get; set; } = string.Empty;
        public int TargetDepartmentId { get; set; }

        public DateTime RequestedAt { get; set; }
        public DateTime? RespondedAt { get; set; }

        public string ResponseNote { get; set; } = string.Empty;
    }

}
