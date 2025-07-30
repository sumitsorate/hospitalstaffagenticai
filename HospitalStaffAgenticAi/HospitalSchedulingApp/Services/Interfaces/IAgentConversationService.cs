using HospitalSchedulingApp.Dal.Entities;

namespace HospitalSchedulingApp.Services.Interfaces
{
    public interface IAgentConversationService
    {
        Task<string?> FetchThreadIdForLoggedInUser();
        Task AddAgentConversation(AgentConversations agentConversations);
    }
}
