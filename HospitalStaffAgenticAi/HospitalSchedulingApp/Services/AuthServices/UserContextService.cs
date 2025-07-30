using HospitalSchedulingApp.Services.AuthServices.Interfaces;
using System.Security.Claims;

namespace HospitalSchedulingApp.Services.AuthServices
{
    public class UserContextService : IUserContextService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserContextService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? GetRole()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;
        }

        public int? GetStaffId()
        {
            var staffIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(staffIdClaim, out var id) ? id : null;
        }

        public bool IsScheduler() => GetRole()?.Equals("Scheduler", StringComparison.OrdinalIgnoreCase) == true;
        public bool IsEmployee() => GetRole()?.Equals("Employee", StringComparison.OrdinalIgnoreCase) == true;
    }
}
