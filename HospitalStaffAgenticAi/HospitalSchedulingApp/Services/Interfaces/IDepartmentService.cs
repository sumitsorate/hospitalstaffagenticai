using HospitalSchedulingApp.Dal.Entities;
using HospitalSchedulingApp.Dtos.Department;

namespace HospitalSchedulingApp.Services.Interfaces
{
    public interface IDepartmentService
    {
        Task<DepartmentDto?> FetchDepartmentInformationAsync(string departmentNamePart);
    }
}
