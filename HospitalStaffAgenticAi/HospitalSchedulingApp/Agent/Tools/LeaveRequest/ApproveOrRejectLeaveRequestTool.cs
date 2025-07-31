using Azure.AI.Agents.Persistent;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Tools.LeaveRequest
{
    public static class ApproveOrRejectLeaveRequestTool
    {
        public static FunctionToolDefinition GetTool()
        {
            return new FunctionToolDefinition(
                name: "approveOrRejectLeaveRequest",
                description: "Approve or reject a leave request. Provide either leaveRequestId directly or use staffId, leaveTypeId, and leave dates to identify the request.",
                parameters: BinaryData.FromObjectAsJson(
                    new
                    {
                        type = "object",
                        required = new[] { "newStatus" },
                        properties = new
                        {
                            leaveRequestId = new
                            {
                                type = "integer",
                                description = "Optional. Leave request ID to directly approve or reject."
                            },
                            staffId = new
                            {
                                type = "integer",
                                description = "Optional. Staff ID used to find the leave request."
                            },
                            leaveTypeId = new
                            {
                                type = "integer",
                                description = "Optional. Leave type ID (e.g., Sick Leave, Casual Leave)."
                            },
                            startDate = new
                            {
                                type = "string",
                                format = "date",
                                description = "Optional. Start date of the leave (yyyy-MM-dd)."
                            },
                            endDate = new
                            {
                                type = "string",
                                format = "date",
                                description = "Optional. End date of the leave (yyyy-MM-dd)."
                            },
                            newStatus = new
                            {
                                type = "string",
                                @enum = new[] { "Approved", "Rejected" },
                                description = "Required. New status to set for the leave request."
                            }
                        }
                    },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                )
            );
        }
    }

}
