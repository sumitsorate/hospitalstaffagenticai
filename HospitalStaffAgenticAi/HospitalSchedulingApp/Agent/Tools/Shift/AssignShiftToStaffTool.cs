using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Dal.Entities;
using Microsoft.Extensions.Options;
using System.Diagnostics.Metrics;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Tools.Shift
{
    public static class AssignShiftToStaffTool
    {
        public static FunctionToolDefinition GetTool()
        {
            return new FunctionToolDefinition(
                name: "assignShiftToStaff",
                description: """
                    Assigns a staff member to a specific planned shift by its ID. 
                    This tool is used to schedule a staff member for a shift that is currently vacant or to reassign an existing shift.

                    ⚠️ Rules to follow:
                    - Never assign the same staff member to more than one shift at the same time.
                    - Avoid assigning staff to back-to-back shifts unless no other suitable staff are available.
                    - Always prefer staff with no adjacent shifts to minimize fatigue and maintain work-life balance.
                    """
,
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
