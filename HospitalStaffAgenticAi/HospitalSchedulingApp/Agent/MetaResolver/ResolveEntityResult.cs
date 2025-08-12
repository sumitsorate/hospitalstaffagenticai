using HospitalSchedulingApp.Common.Enums;
using HospitalSchedulingApp.Dal.Entities;

namespace HospitalSchedulingApp.Agent.MetaResolver
{
    public class DateRange
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
    public class ResolveEntitiesResult
    {
        public Department? Department { get; set; }
        public ShiftType? ShiftType { get; set; }
        public ShiftStatus? ShiftStatus { get; set; }
        public Staff? Staff { get; set; }
        public LeaveStatus?  LeaveStatus { get; set; }
        public LeaveTypes? LeaveType { get; set; }
        public Role? LoggedInUserRole { get; set; }

        public DateRange? DateRange { get; set; }
    }

    public interface IEntityResolver
    {
        Task<ResolveEntitiesResult> ResolveEntitiesAsync(string phrase);
    }

}
