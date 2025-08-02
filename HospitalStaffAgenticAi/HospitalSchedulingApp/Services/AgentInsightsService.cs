using HospitalSchedulingApp.Common.Enums;
using HospitalSchedulingApp.Dal.Entities;
using HospitalSchedulingApp.Dal.Repositories;
using HospitalSchedulingApp.Dtos.Agent;
using HospitalSchedulingApp.Dtos.LeaveRequest.Request;
using HospitalSchedulingApp.Dtos.Shift.Requests;
using HospitalSchedulingApp.Services.Interfaces;

namespace HospitalSchedulingApp.Services
{
    public class AgentInsightsService : IAgentInsightsService
    {
        private readonly IPlannedShiftService _plannedShiftService;
        private readonly ILeaveRequestService _leaveRequestService;

        public AgentInsightsService(
            IPlannedShiftService plannedShiftService,
            ILeaveRequestService leaveRequestService)
        {
            _plannedShiftService = plannedShiftService;
            _leaveRequestService = leaveRequestService;
        }

        public async Task<AgentSummaryResponseDto> GetDailySchedulerSummaryAsync()
        {
            var today = DateTime.Today;
            var shiftFilter = new ShiftFilterDto
            {
                FromDate = today,
                ShiftStatusId = (int)ShiftStatuses.Vacant
            };

            var leaveRequestFilter = new LeaveRequestFilter
            {
                LeaveStatusId = LeaveRequestStatuses.Pending
            };

            var uncoveredShifts = await _plannedShiftService.FetchFilteredPlannedShiftsAsync(shiftFilter);
            var pendingLeaves = await _leaveRequestService.FetchLeaveRequestsAsync(leaveRequestFilter);

            var summaryParts = new List<string>();

            if (uncoveredShifts.Any())
                summaryParts.Add($"• 🕒 {uncoveredShifts.Count} shift{(uncoveredShifts.Count > 1 ? "s" : "")} are currently unassigned and may affect coverage.");

            if (pendingLeaves.Any())
                summaryParts.Add($"• 📥 {pendingLeaves.Count} leave request{(pendingLeaves.Count > 1 ? "s" : "")} are still awaiting your approval.");

            string greeting = GetGreeting();
            string message;

            if (!summaryParts.Any())
            {
                message = $"{greeting} ✅ Everything looks good today! No uncovered shifts or pending leave requests. 👏\n\n"
                        + "👀 You can still:\n"
                        + "• View shift calendar\n"
                        + "• Manage staff availability\n"
                        + "• Review upcoming schedules";
            }
            else
            {
                message = $"{greeting} Here’s a quick summary of today’s staffing status:\n\n"
                        + string.Join("\n", summaryParts)
                        + "\n\n👉 Would you like to take action on any of these?";
            }

            var quickReplies = new List<QuickReply>();

            if (uncoveredShifts.Any())
            {
                quickReplies.Add(new QuickReply
                {
                    Label = "📅 Review Coverage",
                    Value = $"show uncovered shifts for {today:yyyy-MM-dd}"
                });
            }

            if (pendingLeaves.Any())
            {
                quickReplies.Add(new QuickReply
                {
                    Label = "✅ View Pending Leaves",
                    Value = "show/view pending leave requests"
                });
            }

            return new AgentSummaryResponseDto
            {
                SummaryMessage = message,
                QuickReplies = quickReplies
            };
        }

        private string GetGreeting()
        {
            var hour = DateTime.Now.Hour;

            return hour switch
            {
                >= 5 and < 12 => "👋 Good morning!",
                >= 12 and < 17 => "👋 Good afternoon!",
                _ => "👋 Good evening!"
            };
        }
    }

}
