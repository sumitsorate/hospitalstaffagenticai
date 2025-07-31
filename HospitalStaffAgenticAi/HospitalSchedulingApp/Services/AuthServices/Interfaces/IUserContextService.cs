namespace HospitalSchedulingApp.Services.AuthServices.Interfaces
{
    /// <summary>
    /// Defines methods for accessing information about the currently authenticated user.
    /// </summary>
    public interface IUserContextService
    {
        /// <summary>
        /// Gets the role of the currently authenticated user.
        /// </summary>
        /// <returns>The role name (e.g., "Scheduler", "Employee"), or null if not found.</returns>
        string? GetRole();

        /// <summary>
        /// Gets the staff ID of the currently authenticated user.
        /// </summary>
        /// <returns>The staff ID as an integer, or null if it cannot be determined.</returns>
        int? GetStaffId();

        /// <summary>
        /// Determines whether the currently authenticated user has the "Scheduler" role.
        /// </summary>
        /// <returns>True if the user is a Scheduler; otherwise, false.</returns>
        bool IsScheduler();

        /// <summary>
        /// Determines whether the currently authenticated user has the "Employee" role.
        /// </summary>
        /// <returns>True if the user is an Employee; otherwise, false.</returns>
        bool IsEmployee();
    }
}
