using HospitalSchedulingApp.Dal.Entities;
using HospitalSchedulingApp.Dal.Repositories;
using HospitalSchedulingApp.Services.AuthServices.Interfaces;
using HospitalSchedulingApp.Services.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HospitalSchedulingApp.Agent.Services
{
    /// <summary>
    /// Handles agent conversation storage, retrieval, and deletion for the logged-in user.
    /// </summary>
    public class AgentConversationService : IAgentConversationService
    {
        private readonly IRepository<AgentConversations> _agentConversationRepo;
        private readonly IUserContextService _userContextService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AgentConversationService"/> class.
        /// </summary>
        /// <param name="agentConversationRepo">Repository for AgentConversations table.</param>
        /// <param name="userContextService">Service to fetch logged-in user context.</param>
        public AgentConversationService(
            IRepository<AgentConversations> agentConversationRepo,
            IUserContextService userContextService)
        {
            _agentConversationRepo = agentConversationRepo;
            _userContextService = userContextService;
        }

        /// <summary>
        /// Adds a new agent conversation entry and saves it to the database.
        /// </summary>
        /// <param name="agentConversations">The agent conversation to add.</param>
        public async Task AddAgentConversation(AgentConversations agentConversations)
        {
            await _agentConversationRepo.AddAsync(agentConversations);
            await _agentConversationRepo.SaveAsync();
        }

        /// <summary>
        /// Fetches the thread ID associated with the logged-in user, if it exists.
        /// </summary>
        /// <returns>The thread ID as a string, or null if no entry exists.</returns>
        public async Task<string?> FetchThreadIdForLoggedInUser(int? staffId = null)
        {
            int? userId;

            if (staffId.HasValue)
            {
                userId = staffId.Value;
            }
            else
            {
                userId = _userContextService.GetStaffId();
            }

            var agentConversation = (await _agentConversationRepo.GetAllAsync())
                .FirstOrDefault(x => x.UserId == userId.ToString());

            return agentConversation?.ThreadId;
        }


        /// <summary>
        /// Fetches the entire agent conversation info for the logged-in user.
        /// </summary>
        /// <returns>The <see cref="AgentConversations"/> object, or null if not found.</returns>
        public async Task<AgentConversations?> FetchLoggedInUserAgentConversationInfo()
        {
            var userId = _userContextService.GetStaffId();

            var agentConversation = (await _agentConversationRepo.GetAllAsync())
                .FirstOrDefault(x => x.UserId == userId.ToString());

            // May return null if the conversation does not exist
            return agentConversation;
        }

        /// <summary>
        /// Deletes the specified agent conversation from the repository and saves changes.
        /// </summary>
        /// <param name="agentConversation">The agent conversation to delete.</param>
        public async Task DeleteAgentConversation(AgentConversations agentConversation)
        {
            _agentConversationRepo.Delete(agentConversation);
            await _agentConversationRepo.SaveAsync();
        }
    }
}
