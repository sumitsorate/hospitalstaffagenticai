using Azure.AI.Agents.Persistent;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Tools.HelperTools
{
    public static class ResolveStaffReferenceTool
    {
        public static FunctionToolDefinition GetTool()
        {
            return new FunctionToolDefinition(
                name: "resolveStaffReference",
                description: "Resolves a reference in a prompt — a self-reference ('me', 'my shift', 'I am') — to staff ID and name. Use this when the prompt refers to a specific person.",
                parameters: BinaryData.FromObjectAsJson(
                    new
                    {
                        type = "object",
                        properties = new
                        {
                            phrase = new
                            {
                                type = "string",
                                description = "Any phrase that may include a partial name or self-reference like 'me', 'my', 'I am on leave', etc."
                            }
                        },
                        required = new[] { "phrase" }
                    },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                )
            );
        }
    }

}
