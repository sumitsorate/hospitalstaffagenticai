using Azure.AI.Agents.Persistent;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Tools.Shift
{
    public static class FilterShiftScheduleTool
    {
        public static FunctionToolDefinition GetTool()
        {
            return new FunctionToolDefinition(
                name: "filterShiftSchedule",
                description: "Retrieves planned shift schedules using optional filters like department ID, staff ID, shift type ID, shift status ID, and date range. Useful for querying when, where, and what type of shifts are scheduled for specific individuals or departments.",
                parameters: BinaryData.FromObjectAsJson(
                    new
                    {
                        type = "object",
                        properties = new
                        {
                            departmentId = new
                            {
                                type = "integer",
                                description = "Optional. Department ID to filter shift schedule (e.g., 1 for ICU, 2 for OPD)."
                            },
                            staffId = new
                            {
                                type = "integer",
                                description = "Optional. Staff ID to search shifts for a specific staff member."
                            },
                            shiftTypeId = new
                            {
                                type = "integer",
                                description = "Optional. Shift type ID, such as 1 for Morning, 2 for Evening, 3 for Night."
                            },
                            shiftStatusId = new
                            {
                                type = "integer",
                                description = "Optional. Shift status ID like 1 for Scheduled, 2 for Assigned, 3 for Vacant."
                            },
                            fromDate = new
                            {
                                type = "string",
                                format = "date",
                                description = "Optional. Start of the date range (inclusive), in YYYY-MM-DD format."
                            },
                            toDate = new
                            {
                                type = "string",
                                format = "date",
                                description = "Optional. End of the date range (inclusive), in YYYY-MM-DD format."
                            }
                        }
                    },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                )
            );
        }
    }

}
