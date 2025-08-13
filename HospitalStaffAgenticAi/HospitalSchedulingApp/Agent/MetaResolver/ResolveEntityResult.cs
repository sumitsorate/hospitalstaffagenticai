using HospitalSchedulingApp.Common.Enums;
using HospitalSchedulingApp.Dal.Entities;

namespace HospitalSchedulingApp.Agent.MetaResolver
{
    public class ResolveEntitiesResult
    {
        public Department? Department { get; set; }
        public ShiftType? ShiftType { get; set; }
        public ShiftStatus? ShiftStatus { get; set; }
        public LeaveStatus?  LeaveStatus { get; set; }
        public LeaveTypes? LeaveType { get; set; }
        public Role? LoggedInUserRole { get; set; }         
    }

    public interface IEntityResolver
    {
        Task<ResolveEntitiesResult> ResolveEntitiesAsync(string phrase);
    }

}
