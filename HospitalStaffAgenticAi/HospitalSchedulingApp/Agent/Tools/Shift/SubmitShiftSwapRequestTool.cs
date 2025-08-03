using Azure.AI.Agents.Persistent;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Tools.Shift
{ 

    public static class SubmitShiftSwapRequestTool
    {
        public static FunctionToolDefinition GetTool()
        {
            return new FunctionToolDefinition(
                name: "submitShiftSwapRequest",
                description: "Use this tool when a staff member wants to request a shift swap with another staff member. It records the source and target shifts and staff IDs and submits the request with status 'Pending' by default.",
                parameters: BinaryData.FromObjectAsJson(
                    new
                    {
                        type = "object",
                        properties = new
                        {
                            requestingStaffId = new { type = "integer", description = "The ID of the staff member requesting the shift swap." },
                            targetStaffId = new { type = "integer", description = "The ID of the target staff member with whom the swap is requested." },
                            sourceShiftDate = new { type = "string", format = "date", description = "The date of the shift for the requesting staff in yyyy-MM-dd format." },
                            sourceShiftTypeId = new { type = "integer", description = "The shift type ID of the source shift (e.g., Morning, Evening, Night)." },
                            targetShiftDate = new { type = "string", format = "date", description = "The date of the shift for the target staff in yyyy-MM-dd format." },
                            targetShiftTypeId = new { type = "integer", description = "The shift type ID of the target shift." }
                        },
                        required = new[]
                        {
                        "requestingStaffId",
                        "targetStaffId",
                        "sourceShiftDate",
                        "sourceShiftTypeId",
                        "targetShiftDate",
                        "targetShiftTypeId"
                        }
                    },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                )
            );
        }
    }

}
