using HospitalSchedulingApp.Common.Enums;

namespace HospitalSchedulingApp.Dtos.Shift.Requests
{
    public class ShiftSwapDto
    {
        public ShiftSwapStatuses? StatusId { get; set; }

        public int? requesterStaffId { get; set; }

        public int? targetStaffId { get; set; }

        public int? requesterShiftTypeId { get; set; }

        public int? targetShiftTypeId { get; set; }

        public DateTime? fromDate { get; set; }

        public DateTime? toDate { get; set; }

    }
}
