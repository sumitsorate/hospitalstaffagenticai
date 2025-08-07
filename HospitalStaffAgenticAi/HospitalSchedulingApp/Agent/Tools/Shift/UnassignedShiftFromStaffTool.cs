using Azure.AI.Agents.Persistent;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Tools.Shift
{
    public static class UnassignedShiftFromStaffTool
    {
        public static FunctionToolDefinition GetTool()
        {
            return new FunctionToolDefinition(
                name: "unassignShiftFromStaff",
                description: "Unassigns the staff member from a planned shift. Useful when a staff member needs to be removed from a shift (e.g., due to leave or reassignment).",
                parameters: BinaryData.FromObjectAsJson(
                    new
                    {
                        type = "object",
                        properties = new
                        {
                            plannedShiftId = new
                            {
                                type = "integer",
                                description = "The ID of the planned shift to unassign from the staff member."
                            }
                        },
                        required = new[] { "plannedShiftId" }
                    },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                )
            );
        }
    }

}
