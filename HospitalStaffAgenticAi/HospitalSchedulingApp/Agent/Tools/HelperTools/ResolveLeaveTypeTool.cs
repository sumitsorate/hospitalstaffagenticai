using Azure.AI.Agents.Persistent;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Tools.HelperTools
{
    public static class ResolveLeaveTypeTool
    {
        public static FunctionToolDefinition GetTool()
        {
            return new FunctionToolDefinition(
                name: "resolveLeaveType",
                description: "Resolves a natural language leave type input (e.g., 'sick', 'casual', 'vacation') to a standardized leave type enum: Sick, Casual, or Vacation.",
                parameters: BinaryData.FromObjectAsJson(
                    new
                    {
                        type = "object",
                        properties = new
                        {
                            leaveType = new
                            {
                                type = "string",
                                description = "Required. The leave type input to interpret. Examples: 'sick', 'casual' 'vacation' etc."
                            }
                        },
                        required = new[] { "leaveType" }
                    },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                )
            );
        }
    }

}
