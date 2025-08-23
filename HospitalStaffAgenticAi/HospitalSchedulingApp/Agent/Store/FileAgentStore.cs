using HospitalSchedulingApp.Agent.Models;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.AgentStore
{

    /// <summary>
    /// A file-based implementation of <see cref="IAgentStore"/> that stores agent IDs in a JSON file.
    /// This provides a simple persistence mechanism across application restarts.
    /// </summary>
    public class FileAgentStore : IAgentStore
    {
        private readonly string _agentStoreFile = Path.Combine("Config", "agent-config.json");


        /// <summary>
        /// Saves or updates the agent ID associated with the specified agent name to the JSON file.
        /// </summary>
        /// <param name="agentName">The name of the agent.</param>
        /// <param name="agentId">The ID of the agent to persist.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        public async Task SaveAgentIdAsync(string agentName, string agentId)
        {
            if (!File.Exists(_agentStoreFile))
                throw new FileNotFoundException($"Agent config file not found: {_agentStoreFile}");

            var json = await File.ReadAllTextAsync(_agentStoreFile);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Copy the whole config to a mutable dictionary
            var configDict = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new();

            // If "agent" section exists, update assistanceId
            if (root.TryGetProperty("agent", out var agentElement))
            {
                var agentDict = JsonSerializer.Deserialize<Dictionary<string, object>>(agentElement.GetRawText()) ?? new();
                agentDict["assistanceId"] = agentId;
                configDict["agent"] = agentDict;
            }
            else
            {
                // If no "agent" section, create one
                configDict["agent"] = new Dictionary<string, object> {
                    { "assistanceId", agentId }
                 };
            }

            var updatedJson = JsonSerializer.Serialize(configDict, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_agentStoreFile, updatedJson);
        }

        /// <summary>
        /// Fetches agent information from the config/agent-config.json file and returns an AgentDefinition.
        /// </summary>
        /// <returns>
        /// A task representing the asynchronous operation. The result contains the AgentDefinition if found, or <c>null</c> otherwise.
        /// </returns>
        public async Task<AgentDefinition?> FetchAgentInformation()
        {
            if (!File.Exists(_agentStoreFile))
                return null;

            var json = await File.ReadAllTextAsync(_agentStoreFile);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("agent", out var agentElement))
                return null;

            var agentDefinition = agentElement.Deserialize<AgentDefinition>();

            if (agentDefinition == null)
                return null;

            string instructions = string.Empty;
            foreach (var item in agentDefinition.SystemPromptPaths)
            {
                instructions += await File.ReadAllTextAsync(Path.Combine("SystemPrompt", item)) + Environment.NewLine;
            }

            agentDefinition.Instructions = instructions;

            return agentDefinition;
        }
    }
}
