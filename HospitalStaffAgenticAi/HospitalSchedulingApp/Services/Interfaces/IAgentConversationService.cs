using HospitalSchedulingApp.Dal.Entities;

namespace HospitalSchedulingApp.Services.Interfaces
{
    /// <summary>
    /// Provides methods for managing agent conversations per logged-in user.
    /// </summary>
    public interface IAgentConversationService
    {
        /// <summary>
        /// Fetches the persisted agent thread ID for the currently logged-in user.
        /// </summary>
        /// <returns>The thread ID if found; otherwise, null.</returns>
        Task<string?> FetchThreadIdForLoggedInUser();

        /// <summary>
        /// Adds a new agent conversation record to the database.
        /// </summary>
        /// <param name="agentConversations">The agent conversation entity to add.</param>
        Task AddAgentConversation(AgentConversations agentConversations);

        /// <summary>
        /// Retrieves the agent conversation information for the currently logged-in user.
        /// </summary>
        /// <returns>The corresponding <see cref="AgentConversations"/> entity if it exists; otherwise, null.</returns>
        Task<AgentConversations?> FetchLoggedInUserAgentConversationInfo();

        /// <summary>
        /// Deletes the specified agent conversation from the database.
        /// </summary>
        /// <param name="agentConversation">The agent conversation entity to delete.</param>
        Task DeleteAgentConversation(AgentConversations agentConversation);
    }
}
