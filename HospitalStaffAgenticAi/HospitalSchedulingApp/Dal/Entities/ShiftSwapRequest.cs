using HospitalSchedulingApp.Common.Enums;

namespace HospitalSchedulingApp.Dal.Entities
{
    public class ShiftSwapRequest
    {
        public int Id { get; set; }

        public int RequestingStaffId { get; set; }
        public int TargetStaffId { get; set; }

        public DateTime SourceShiftDate { get; set; }
        public int SourceShiftTypeId { get; set; }

        public DateTime TargetShiftDate { get; set; }
        public int TargetShiftTypeId { get; set; }

        public ShiftSwapStatuses StatusId { get; set; }

        public DateTime RequestedAt { get; set; }
        public DateTime? RespondedAt { get; set; }

        public string ResponseNote { get; set; } = string.Empty;
    }

}
