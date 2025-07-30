namespace HospitalSchedulingApp.Services.AuthServices.Interfaces
{
    public interface IUserContextService
    {
        string? GetRole();
        int? GetStaffId();
        bool IsScheduler();
        bool IsEmployee();
    }
}
