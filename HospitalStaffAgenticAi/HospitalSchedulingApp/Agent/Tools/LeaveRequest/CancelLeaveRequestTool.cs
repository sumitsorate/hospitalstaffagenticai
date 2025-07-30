using Azure.AI.Agents.Persistent;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Tools.LeaveRequest
{
    public static class CancelLeaveRequestTool
    {
        public static FunctionToolDefinition GetTool()
        {
            return new FunctionToolDefinition(
                name: "cancelLeaveRequest",
                description: "Use this tool when a staff member wants to cancel a previously submitted leave request. It identifies the leave based on staff ID and date range and marks it as 'Cancelled' if found.",
                parameters: BinaryData.FromObjectAsJson(
                    new
                    {
                        type = "object",
                        properties = new
                        {
                            staffId = new { type = "integer", description = "The ID of the staff whose leave request is to be cancelled." },
                            leaveStart = new { type = "string", format = "date", description = "Leave start date in yyyy-MM-dd format of the leave to cancel." },
                            leaveEnd = new { type = "string", format = "date", description = "Leave end date in yyyy-MM-dd format of the leave to cancel." },
                        },
                        required = new[] { "staffId", "leaveStart", "leaveEnd" }
                    },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                )
            );
        }
    }
}
