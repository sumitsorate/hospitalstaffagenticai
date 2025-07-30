using Azure.AI.Agents.Persistent;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Tools.HelperTools
{

    public static class ResolveShiftStatusTool
    {
        public static FunctionToolDefinition GetTool()
        {
            return new FunctionToolDefinition(
                name: "resolveShiftStatus",
                description: "Resolves a natural language shift status input (e.g., 'scheduled', 'vacant', 'cancelled') to a standardized shift status enum: Scheduled, Assigned, Completed, Cancelled, or Vacant.",
                parameters: BinaryData.FromObjectAsJson(
                    new
                    {
                        type = "object",
                        properties = new
                        {
                            status = new
                            {
                                type = "string",
                                description = "Required. The shift status input to interpret. Examples: 'scheduled', 'vacant', 'cancelled', 'assigned', 'completed', etc."
                            }
                        },
                        required = new[] { "status" }
                    },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                )
            );
        }
    }
}
