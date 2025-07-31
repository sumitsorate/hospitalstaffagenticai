using Azure.AI.Agents.Persistent;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Tools.LeaveRequest
{
    public static class FetchLeaveRequestTool
    {
        public static FunctionToolDefinition GetTool()
        {
            return new FunctionToolDefinition(
                name: "fetchLeaveRequest",
                description: "Use this tool to retrieve leave requests using optional filters like staff ID, leave status, leave type, or date range.",
                parameters: BinaryData.FromObjectAsJson(
                    new
                    {
                        type = "object",
                        properties = new
                        {
                            staffId = new
                            {
                                type = "integer",
                                description = "Optional. Filter by the staff member's ID."
                            },
                            leaveStatusId = new
                            {
                                type = "integer",
                                description = "Optional. Filter by leave status: 'Pending', 'Approved', or 'Rejected'.",                               
                            },
                            startDate = new
                            {
                                type = "string",
                                format = "date",
                                description = "Optional. Start date of the leave range filter (yyyy-MM-dd)."
                            },
                            endDate = new
                            {
                                type = "string",
                                format = "date",
                                description = "Optional. End date of the leave range filter (yyyy-MM-dd)."
                            },
                            leaveTypeId = new
                            {
                                type = "integer",
                                description = "Optional. Filter by leave type: 'Sick', 'Vacation', 'Emergency', etc."
                            }
                        }
                    },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                )
            );
        }
    }


}
