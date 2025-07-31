using HospitalSchedulingApp.Services.AuthServices.Interfaces;
using System.Security.Claims;

namespace HospitalSchedulingApp.Services.AuthServices
{
    /// <summary>
    /// Provides methods to retrieve the current user's context such as role and staff ID
    /// from the HTTP context.
    /// </summary>
    public class UserContextService : IUserContextService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserContextService"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">Provides access to the current HTTP context.</param>
        public UserContextService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Retrieves the role of the currently logged-in user.
        /// </summary>
        /// <returns>The role as a string (e.g., "Scheduler", "Employee"); otherwise, null.</returns>
        public string? GetRole()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;
        }

        /// <summary>
        /// Retrieves the staff ID of the currently logged-in user.
        /// </summary>
        /// <returns>The staff ID if it can be parsed; otherwise, null.</returns>
        public int? GetStaffId()
        {
            var staffIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(staffIdClaim, out var id) ? id : null;
        }

        /// <summary>
        /// Determines whether the current user has the "Scheduler" role.
        /// </summary>
        /// <returns>True if the user is a Scheduler; otherwise, false.</returns>
        public bool IsScheduler() =>
            GetRole()?.Equals("Scheduler", StringComparison.OrdinalIgnoreCase) == true;

        /// <summary>
        /// Determines whether the current user has the "Employee" role.
        /// </summary>
        /// <returns>True if the user is an Employee; otherwise, false.</returns>
        public bool IsEmployee() =>
            GetRole()?.Equals("Employee", StringComparison.OrdinalIgnoreCase) == true;
    }
}
