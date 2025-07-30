using Azure.AI.Agents.Persistent;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Tools.LeaveRequest
{
    public static class SubmitLeaveRequestTool
    {
        public static FunctionToolDefinition GetTool()
        {
            return new FunctionToolDefinition(
                name: "submitLeaveRequest",
                description: "Use this tool when a staff member applies for leave or mentions being unavailable on specific dates. It submits a new leave request with the start and end dates and leave type. The leave request is submitted with status 'Pending' by default.",
                parameters: BinaryData.FromObjectAsJson(
                    new
                    {
                        type = "object",
                        properties = new
                        {
                            staffId = new { type = "integer", description = "The ID of the staff applying for leave." },
                            leaveStart = new { type = "string", format = "date", description = "Leave start date in yyyy-MM-dd format." },
                            leaveEnd = new { type = "string", format = "date", description = "Leave end date in yyyy-MM-dd format." },
                            leaveType = new { type = "string", description = "Type of leave (e.g., Sick, Vacation, Emergency)." }
                        },
                        required = new[] { "staffId", "leaveStart", "leaveEnd", "leaveType" }
                    },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                )
            );
        }
    }

}
