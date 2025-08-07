using Azure.AI.Agents.Persistent;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Tools.Shift
{
    public static class AddNewPlannedShiftTool
    {
        public static FunctionToolDefinition GetTool()
        {
            return new FunctionToolDefinition(
                name: "addNewPlannedShift",
                description: "Adds a new vacant planned shift for a given date, shift type, department, and slot number. Used for scheduling shifts in advance without assigning staff.",
                parameters: BinaryData.FromObjectAsJson(
                    new
                    {
                        type = "object",
                        properties = new
                        {
                            shiftDate = new
                            {
                                type = "string",
                                format = "date",
                                description = "The date of the shift in yyyy-MM-dd format (e.g., 2025-08-10)."
                            },
                            shiftTypeId = new
                            {
                                type = "integer",
                                description = "The shift type ID (e.g., 1 for Morning, 2 for Evening, 3 for Night)."
                            },
                            departmentId = new
                            {
                                type = "integer",
                                description = "The department ID where the shift will be scheduled."
                            },
                            slotNumber = new
                            {
                                type = "integer",
                                description = "The slot number for the shift (used to distinguish multiple shifts of the same type on the same day)."
                            }
                        },
                        required = new[] { "shiftDate", "shiftTypeId", "departmentId", "slotNumber" }
                    },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                )
            );
        }
    }

}
