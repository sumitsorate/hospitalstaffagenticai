using Azure.AI.Agents.Persistent;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Tools.Shift
{
    public class FetchShiftSwapRequestTool
    {
        public static FunctionToolDefinition GetTool()
        {
            return new FunctionToolDefinition(
                name: "fetchShiftSwapRequest",
                description: "Use this tool to get the list of shift swap requests",
                parameters: BinaryData.FromObjectAsJson(new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            );
        }
    }
}
