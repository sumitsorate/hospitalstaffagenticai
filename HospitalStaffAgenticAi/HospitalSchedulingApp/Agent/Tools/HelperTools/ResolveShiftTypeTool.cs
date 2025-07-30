using Azure.AI.Agents.Persistent;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Tools.HelperTools
{
    
    public static class ResolveShiftTypeTool
    {
        public static FunctionToolDefinition GetTool()
        {
            return new FunctionToolDefinition(
                name: "resolveShiftType",
                description: "Resolves a user-provided shift type (like 'night', 'morning', or 'evening') into a standard shift type enum value.",
                parameters: BinaryData.FromObjectAsJson(
                    new
                    {
                        type = "object",
                        properties = new
                        {
                            shift = new
                            {
                                type = "string",
                                description = "The shift type to resolve. Examples: 'night', 'morning', 'evening', or abbreviations like 'n', 'm', 'e'."
                            }
                        },
                        required = new[] { "shift" }
                    },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                )
            );
        }
    }
}
