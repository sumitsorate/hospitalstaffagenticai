using System.Text.Json.Serialization;

namespace HospitalSchedulingApp.Agent.Models
{
    public class AgentDefinition
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("systemPromptPaths")]
        public string[] SystemPromptPaths { get; set; } = new string[] { };

        [JsonPropertyName("modelDeploymentName")]
        public string ModelDeploymentName { get; set; } = string.Empty;

        [JsonPropertyName("assistanceId")]
        public string? AssistanceId { get; set; } = string.Empty;


        public string? Instructions { get; set; } = string.Empty;
    }
}
