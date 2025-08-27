using HospitalSchedulingApp.Common.Exceptions;
using HospitalSchedulingApp.Dal.Entities;
using HospitalSchedulingApp.Dal.Repositories;
using HospitalSchedulingApp.Dtos.Department;
using HospitalSchedulingApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HospitalSchedulingApp.Services
{
    /// <summary>
    /// Service for managing and fetching department information.
    /// </summary>
    public class DepartmentService : IDepartmentService
    {
        private readonly IRepository<Department> _departmentRepo;
        private readonly ILogger<DepartmentService> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="DepartmentService"/>.
        /// </summary>
        /// <param name="departmentRepo">Repository for Department entity.</param>
        /// <param name="logger">Logger instance for structured logging.</param>
        public DepartmentService(
            IRepository<Department> departmentRepo,
            ILogger<DepartmentService> logger)
        {
            _departmentRepo = departmentRepo ?? throw new ArgumentNullException(nameof(departmentRepo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Fetch department information by (partial) department name.
        /// </summary>
        /// <param name="departmentNamePart">Full or partial name of the department.</param>
        /// <returns>A matching <see cref="DepartmentDto"/> or null if not found.</returns>
        /// <exception cref="BusinessRuleException">Thrown when input is invalid.</exception>
        public async Task<DepartmentDto?> FetchDepartmentInformationAsync(string departmentNamePart)
        {
            if (string.IsNullOrWhiteSpace(departmentNamePart))
                throw new BusinessRuleException("❌ Department name cannot be null or empty.");

            try
            {
                _logger.LogInformation("Fetching department info for input '{Input}'", departmentNamePart);

                // ⚡ Prefer pushing filter to DB if repository supports IQueryable
                var departments = await _departmentRepo.GetAllAsync();

                var department = departments
                    .FirstOrDefault(d =>
                        d.DepartmentName != null &&
                        d.DepartmentName.Contains(departmentNamePart.Trim(), StringComparison.OrdinalIgnoreCase));

                if (department == null)
                {
                    _logger.LogWarning("No department found matching input '{Input}'", departmentNamePart);
                    return null;
                }

                var dto = new DepartmentDto
                {
                    DepartmentId = department.DepartmentId,
                    DepartmentName = department.DepartmentName
                };

                _logger.LogInformation("Department resolved: {Id} - {Name}", dto.DepartmentId, dto.DepartmentName);

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching department info for '{Input}'", departmentNamePart);
                throw; // rethrow for upper layers (e.g., handler) to manage
            }
        }
    }
}
