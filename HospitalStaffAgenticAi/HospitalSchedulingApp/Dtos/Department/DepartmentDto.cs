namespace HospitalSchedulingApp.Dtos.Department
{
    /// <summary>
    /// Represents a department within the hospital, 
    /// including its unique identifier and display name.
    /// </summary>
    public class DepartmentDto
    {
        /// <summary>
        /// Gets or sets the unique identifier for the department.
        /// </summary>
        public int DepartmentId { get; set; }

        /// <summary>
        /// Gets or sets the display name of the department.
        /// </summary>
        public string DepartmentName { get; set; } = string.Empty;
    }
}
