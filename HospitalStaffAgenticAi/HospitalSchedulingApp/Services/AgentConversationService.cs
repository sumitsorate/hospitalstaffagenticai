using HospitalSchedulingApp.Dal.Entities;
using HospitalSchedulingApp.Dal.Repositories;
using HospitalSchedulingApp.Services.AuthServices.Interfaces;
using HospitalSchedulingApp.Services.Interfaces;

namespace HospitalSchedulingApp.Services
{
    public class AgentConversationService : IAgentConversationService
    {
        private readonly IRepository<AgentConversations> _agentConversationRepo;
        private readonly IUserContextService _userContextService;

        public AgentConversationService(
            IRepository<AgentConversations> agentConversationRepo,
            IUserContextService userContextService)
        {
            _agentConversationRepo = agentConversationRepo;
            _userContextService = userContextService;
        }

        public async Task AddAgentConversation(AgentConversations agentConversations)
        {
            await _agentConversationRepo.AddAsync(agentConversations);
            await _agentConversationRepo.SaveAsync();            
        }

        public async Task<string?> FetchThreadIdForLoggedInUser()
        {
            var userId = _userContextService.GetStaffId();
            var agentConversation = (await _agentConversationRepo.GetAllAsync())
                .FirstOrDefault(x => x.UserId == userId.ToString());

            return agentConversation?.ThreadId ?? null ;
        }
    }

}
