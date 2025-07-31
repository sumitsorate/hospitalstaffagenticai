using HospitalSchedulingApp.Common.Enums;

namespace HospitalSchedulingApp.Dtos.LeaveRequest.Response
{

    public class LeaveRequestDetailsDto
    {
        public int LeaveRequestId { get; set; }
        public int StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;

        public int StaffDepartmentId { get; set; }
        public string StaffDepartmentName { get; set; } = string.Empty;

        public DateTime LeaveStart { get; set; }
        public DateTime LeaveEnd { get; set; }

        public LeaveRequestStatuses LeaveStatus { get; set; }

        public string LeaveStatusName { get; set; } = string.Empty;

        public LeaveType LeaveTypeId { get; set; }
        public string LeaveTypeName { get; set; } = string.Empty;
         
    }

}
