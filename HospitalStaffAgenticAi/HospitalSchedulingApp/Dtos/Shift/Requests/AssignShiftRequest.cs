namespace HospitalSchedulingApp.Dtos.Shift.Requests
{
    public class AssignShiftRequest
    {
        /// <summary>
        /// ID of the staff member to assign.
        /// </summary>
        public int StaffId { get; set; }

        /// <summary>
        /// Date of the shift to assign (format: yyyy-MM-dd).
        /// </summary>
        public DateOnly ShiftDate { get; set; }

        /// <summary>
        /// Type of the shift (e.g., Morning, Evening, Night).
        /// </summary>
        public int ShiftTypeId { get; set; }

        /// <summary>
        /// Department in which the shift exists.
        /// </summary>
        public int DepartmentId { get; set; }

        /// <summary>
        /// Optional: If multiple slots exist for same department and type.
        /// </summary>
        public int? SlotNumber { get; set; }
    }

}
