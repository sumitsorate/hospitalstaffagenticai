using Azure.AI.Agents.Persistent;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Tools.LeaveRequest
{
    public static class ResolveLeaveStatusTool
    {
        public static FunctionToolDefinition GetTool()
        {
            return new FunctionToolDefinition(
                name: "resolveLeaveStatus",
                description: "Resolves a natural language leave status input (e.g., 'awaiting', 'denied', 'granted','approve'," +
                "'approved', 'deny' , 'refuse','approved','rejected') to a standardized leave status enum: Pending, Approved, or Rejected.",
                parameters: BinaryData.FromObjectAsJson(
                    new
                    {
                        type = "object",
                        properties = new
                        {
                            status = new
                            {
                                type = "string",
                                description = "Required. The leave status input to interpret. Examples: 'awaiting', 'denied', 'granted','approve'," +
                             "'approved', 'deny' , 'refuse','approved','rejected', etc."
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
