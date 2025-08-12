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
                    "Use this tool when the user provides a date or date range in natural language format that is not already in ISO format (yyyy-MM-dd). " +
                    "Examples: '1st Aug 2025', 'Aug 1st', 'August 1', '01/08/2025', 'Friday 1st Aug', '8-1-2025', '14 Aug to 16 Aug', '14 Aug 2025 to 16 Aug 2025', '15/8 - 14/8'. " +
                    "These formats are common in user messages but must be normalized to ISO dates before tool usage. " +
                    "If a range is given (e.g., '14 to 16 Aug'), resolve both startDate and endDate in ISO format. " +
                    "If the range is reversed (e.g., '16 Aug to 14 Aug'), swap so startDate is earlier. " +
                    "If only one date is given, set startDate and endDate to that date. " +
                    "Do NOT use this tool for vague phrases like 'next week', 'tomorrow', or 'this weekend' — use resolveRelativeDate for those cases instead.",
                parameters: BinaryData.FromObjectAsJson(
                    new
                    {
                        type = "object",
                        properties = new
                        {
                            naturalDate = new
                            {
                                type = "string",
                                description = "The natural language date input to resolve. Example: '1st Aug 2025', '8-1-2025', '14 Aug to 16 Aug',  '14 Aug 2025 to 16 Aug 2025', etc."
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
