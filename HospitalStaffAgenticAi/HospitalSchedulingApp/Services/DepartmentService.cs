using HospitalSchedulingApp.Dal.Entities;
using HospitalSchedulingApp.Dal.Repositories;
using HospitalSchedulingApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HospitalSchedulingApp.Services
{
    public class DepartmentService : IDepartmentService
    {
        private readonly IRepository<Department> _departmentRepo;

        public DepartmentService(IRepository<Department> departmentRepo)
        {
            _departmentRepo = departmentRepo ?? throw new ArgumentNullException(nameof(departmentRepo));
        }
        public async Task<Department?> FetchDepartmentInformationAsync(string departmentNamePart)
        {
            if (string.IsNullOrWhiteSpace(departmentNamePart))
                throw new ArgumentException("Department name part cannot be null or empty.", nameof(departmentNamePart));

            var departments = await _departmentRepo.GetAllAsync();

            var tokens = departmentNamePart
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .ToArray();

            // Find first department whose name contains any token (case-insensitive)
            var department = departments
                .FirstOrDefault(d => tokens.Any(token =>
                    d.DepartmentName?.Contains(token, StringComparison.OrdinalIgnoreCase) == true));

            //return department;

            //var department = departments
            //    .FirstOrDefault(d => d.DepartmentName?.Contains(departmentNamePart.Trim(),
            //    StringComparison.OrdinalIgnoreCase) == true);


            return department;
        }
    }
}
