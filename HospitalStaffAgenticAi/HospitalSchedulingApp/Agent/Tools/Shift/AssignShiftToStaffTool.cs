using Azure.AI.Agents.Persistent;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Tools.Shift
{

    public static class AssignShiftToStaffTool
    {
        public static FunctionToolDefinition GetTool()
        {
            return new FunctionToolDefinition(
                name: "assignShiftToStaff",
                description: "Assigns a staff member to a planned shift by ID. Useful for scheduling staff for a shift that is currently vacant or reassigning an occupied one.",
                parameters: BinaryData.FromObjectAsJson(
                    new
                    {
                        type = "object",
                        properties = new
                        {
                            plannedShiftId = new
                            {
                                type = "integer",
                                description = "The ID of the planned shift to which the staff should be assigned."
                            },
                            staffId = new
                            {
                                type = "integer",
                                description = "The ID of the staff member to assign to the shift."
                            }
                        },
                        required = new[] { "plannedShiftId", "staffId" }
                    },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                )
            );
        }
    }

}
