using Azure.AI.Agents.Persistent;
using System.Text.Json;

namespace HospitalSchedulingApp.Agent.Tools.HelperTools
{
   

    public static class ResolveNaturalLanguageDateTool
    {
        public static FunctionToolDefinition GetTool()
        {
            return new FunctionToolDefinition(
                name: "resolveNaturalLanguageDate",
                description:
                    "Use this tool when the user provides a date in natural language format that is not already in ISO format (yyyy-MM-dd). " +
                    "Examples: '1st Aug 2025', 'Aug 1st', 'August 1', '01/08/2025', 'Friday 1st Aug', or '8-1-2025'. " +
                    "These formats are common in user messages but must be normalized to yyyy-MM-dd before tool usage. " +
                    "Do NOT use this tool for vague phrases like 'next week', 'tomorrow', or 'this weekend' — use resolveRelativeDate for those cases instead. " +
                    "The output will be a valid ISO 8601 date in 'yyyy-MM-dd' format.",
                parameters: BinaryData.FromObjectAsJson(
                    new
                    {
                        type = "object",
                        properties = new
                        {
                            naturalDate = new
                            {
                                type = "string",
                                description = "The natural language date input to resolve. Example: '1st Aug 2025', '8-1-2025', 'Aug 1', etc."
                            }
                        },
                        required = new[] { "naturalDate" }
                    },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                )
            );
        }
    }

}
