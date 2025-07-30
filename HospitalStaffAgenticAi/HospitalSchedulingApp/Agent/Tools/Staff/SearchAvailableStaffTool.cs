using Azure.AI.Agents.Persistent;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Tools.Staff
{
    public static class SearchAvailableStaffTool
    {
        public static FunctionToolDefinition GetTool()
        {
            return new FunctionToolDefinition(
                name: "searchAvailableStaff",
                description: "Finds staff members who are free and eligible to work during a given date or date range, filtered by shift type ID and optionally by department ID. "
                           + "Validates staff availability, approved leaves, and existing shift assignments to suggest only suitable and unassigned staff. "
                           + "Supports both planning future shifts and replacing staff in existing ones.",
                parameters: BinaryData.FromObjectAsJson(
                    new
                    {
                        type = "object",
                        properties = new
                        {
                            startDate = new
                            {
                                type = "string",
                                format = "date",
                                description = "Required. Start date of the availability window (YYYY-MM-DD). Can be inferred from natural language like 'tomorrow', 'next week', etc."
                            },
                            endDate = new
                            {
                                type = "string",
                                format = "date",
                                description = "Required. End date of the availability window (YYYY-MM-DD). If same as startDate, it's treated as a single-day query."
                            },
                            shiftTypeId = new
                            {
                                type = "integer",
                                description = "Optional. ID of the shift type (e.g., Morning = 1, Evening = 2, Night = 3)."
                            },
                            departmentId = new
                            {
                                type = "integer",
                                description = "Optional. Department ID to filter or prioritize staff from a specific department (e.g., ICU = 1, Emergency = 2)."
                            }
                        },
                        required = new[] { "startDate", "endDate" }
                    },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                )
            );
        }
    }
}
