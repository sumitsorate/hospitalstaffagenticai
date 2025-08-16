using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.AgentStore;
using HospitalSchedulingApp.Agent.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace HospitalSchedulingApp.Agent
{
    /// <summary>
    /// Manages the lifecycle of a persistent Azure OpenAI agent.
    /// Handles creation, retrieval, and persistence of the agent ID for reuse.
    /// </summary>
    public class AgentManager : IAgentManager
    {
        private readonly PersistentAgentsClient _client;
        private readonly IConfiguration _config;
        private readonly ILogger<AgentManager> _logger;
        private readonly IAgentStore _agentStore;
        private readonly string _agentName;
        private string agentId = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="AgentManager"/> class.
        /// </summary>
        /// <param name="persistentAgentClient">The Azure OpenAI persistent agents client.</param>
        /// <param name="config">Application configuration (for model and agent details).</param>
        /// <param name="logger">Logging provider.</param>
        /// <param name="agentStore">Persistence provider for agent IDs.</param>
        /// <exception cref="ArgumentNullException">Thrown if required dependencies or config values are missing.</exception>
        public AgentManager(
            PersistentAgentsClient persistentAgentClient,
            IConfiguration config,
            ILogger<AgentManager> logger,
            IAgentStore agentStore)
        {
            _client = persistentAgentClient ?? throw new ArgumentNullException(nameof(persistentAgentClient));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _agentStore = agentStore ?? throw new ArgumentNullException(nameof(agentStore));

            _agentName = _config["AgentName"]
                ?? throw new ArgumentNullException("AgentName configuration is missing.");
        }

        /// <summary>
        /// Retrieves the persistent agent for the current session.
        /// </summary>
        /// <returns>The <see cref="PersistentAgent"/> instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the agent has not been initialized yet.</exception>
        public PersistentAgent GetAgent()
        {
            if (string.IsNullOrWhiteSpace(agentId))
            {
                _logger.LogError("AgentId is null or empty. EnsureAgentExistsAsync() must be called first.");
                throw new InvalidOperationException("Agent has not been initialized. Call EnsureAgentExistsAsync() first.");
            }

            try
            {
                _logger.LogInformation("Fetching agent with ID: {AgentId}", agentId);

                var agent = _client.Administration.GetAgent(agentId);

                if (agent == null)
                {
                    _logger.LogWarning("No agent found for AgentId: {AgentId}. It may have been deleted.", agentId);
                    throw new InvalidOperationException($"Agent with ID {agentId} was not found.");
                }

                return agent;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error occurred while fetching agent {AgentId}.", agentId);
                throw new InvalidOperationException(
                    "Could not fetch the agent due to a network error. See inner exception for details.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while fetching agent {AgentId}.", agentId);
                throw;
            }
        }

        /// <summary>
        /// Ensures that a persistent agent exists by:
        /// 1. Reusing a stored agent ID if valid.
        /// 2. Searching for an agent by name.
        /// 3. Creating a new agent if none exists.
        /// Saves the agent ID for future reuse.
        /// </summary>
        /// <returns>The active <see cref="PersistentAgent"/> instance.</returns>
        public async Task<PersistentAgent> EnsureAgentExistsAsync()
        {
            // Try stored agent ID first
            agentId = await _agentStore.LoadAgentIdAsync(_agentName) ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(agentId))
            {
                try
                {
                    var testAgent = _client.Administration.GetAgent(agentId);
                    _logger.LogInformation("Using previously stored agent ID: {AgentId}", agentId);
                    return testAgent;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Stored agent ID {AgentId} is invalid or unreachable.", agentId);
                }
            }

            // Search for existing agent by name
            _logger.LogInformation("Searching for agent with name: {AgentName}", _agentName);

            await foreach (var agent in _client.Administration.GetAgentsAsync())
            {
                if (string.Equals(agent.Name, _agentName, StringComparison.OrdinalIgnoreCase))
                {
                    agentId = agent.Id;
                    _logger.LogInformation("Found existing agent with ID: {AgentId}", agentId);
                    await _agentStore.SaveAgentIdAsync(_agentName, agentId);
                    return agent;
                }
            }

            // Create a new agent
            string systemPromptPath = Path.Combine("SystemPrompt", "SystemPrompt.txt");

            if (!File.Exists(systemPromptPath))
            {
                _logger.LogError("System prompt file not found at: {Path}", systemPromptPath);
                throw new FileNotFoundException($"System prompt file not found at: {systemPromptPath}");
            }

            string instructions = await File.ReadAllTextAsync(systemPromptPath);
            string modelDeployment = _config["ModelDeploymentName"]
                ?? throw new ArgumentNullException("ModelDeploymentName configuration is missing.");

            var newAgentResponse = await _client.Administration.CreateAgentAsync(
                model: modelDeployment,
                name: _agentName,
                instructions: instructions,
                tools: ToolDefinitions.All
            );

            var newAgent = newAgentResponse?.Value
                ?? throw new InvalidOperationException("Failed to create agent: response was null.");

            agentId = newAgent.Id;
            _logger.LogInformation("Created new agent with ID: {AgentId}", agentId);

            await _agentStore.SaveAgentIdAsync(_agentName, agentId);

            return newAgent;
        }
    }
}
