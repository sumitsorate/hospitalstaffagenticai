using HospitalSchedulingApp.Agent.Models;

namespace HospitalSchedulingApp.Agent.AgentStore
{
    /// <summary>
    /// Defines a contract for storing and retrieving persistent agent IDs by name.
    /// This interface is useful for maintaining agent continuity across application restarts.
    /// </summary>
    public interface IAgentStore
    {

        /// <summary>
        /// Asynchronously saves the agent ID associated with the specified agent name.
        /// </summary>
        /// <param name="agentName">The name of the agent.</param>
        /// <param name="agentId">The ID of the agent to store.</param>
        /// <returns>A task that represents the asynchronous save operation.</returns>
        Task SaveAgentIdAsync(string agentName, string agentId);

        /// <summary>
        /// Fetches agent information from json file.
        /// </summary>        
        /// <returns>A task that represents the asynchronous save operation.</returns>
        Task<AgentDefinition?> FetchAgentInformation();

    }
}
