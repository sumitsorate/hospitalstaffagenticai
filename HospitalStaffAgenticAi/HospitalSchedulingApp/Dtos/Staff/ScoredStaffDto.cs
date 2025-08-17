namespace HospitalSchedulingApp.Dtos.Staff
{
    /// <summary>
    /// Represents a staff member with an associated score and reasoning
    /// for ranking or selection purposes.
    /// </summary>
    public class ScoredStaffDto : StaffDto
    {
        /// <summary>
        /// Gets or sets the calculated score (normalized between 0 and 1).
        /// Higher values indicate a stronger match.
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        /// Gets or sets the explanation of how the score was derived.
        /// Useful for debugging, logging, or explainable AI scenarios.
        /// </summary>
        public string Reasoning { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the staff member
        /// is at risk of fatigue (e.g., insufficient rest between shifts).
        /// </summary>
        public bool IsFatigueRisk { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the staff member
        /// is from a different department than requested.
        /// </summary>
        public bool IsCrossDepartment { get; set; }


        /// <summary>
        /// Gets or sets a value indicating whether the staff member
        /// is from a different department than requested.
        /// </summary>
        public bool IsBackToBackRisk { get; set; }

        
    }

}
