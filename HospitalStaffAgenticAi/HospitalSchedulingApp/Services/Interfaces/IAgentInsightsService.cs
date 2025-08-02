using HospitalSchedulingApp.Dtos.Agent;

namespace HospitalSchedulingApp.Services.Interfaces
{
    public interface IAgentInsightsService
    {
        Task<AgentSummaryResponseDto?> GetDailySchedulerSummaryAsync();
    }
}
