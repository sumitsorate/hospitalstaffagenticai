using System;

namespace HospitalSchedulingApp.Dtos.Staff.Requests
{
    /// <summary>
    /// Represents filtering criteria for searching available staff 
    /// for a specific date range, shift type, and optional department and role.
    /// </summary>
    public class AvailableStaffFilterDto
    {
        /// <summary>
        /// The start date (inclusive) of the shift range to search availability for.
        /// </summary>
        public DateOnly StartDate { get; set; }

        /// <summary>
        /// The end date (inclusive) of the shift range to search availability for.
        /// </summary>
        public DateOnly EndDate { get; set; }

        /// <summary>
        /// The type of shift to filter by (e.g., "Morning", "Evening", "Night").
        /// </summary>
        public int? ShiftTypeId { get; set; }

        /// <summary>
        /// Optional department name to prioritize staff from a specific department.
        /// </summary>
        public int? DepartmentId { get; set; }

        /// <summary>
        /// If true, skips suggesting staff who may face fatigue risks (back-to-back shifts).
        /// If false, allows them but typically only after scheduler confirmation.
        /// </summary>
        public bool ApplyFatigueCheck { get; set; } = true;

    }
}
