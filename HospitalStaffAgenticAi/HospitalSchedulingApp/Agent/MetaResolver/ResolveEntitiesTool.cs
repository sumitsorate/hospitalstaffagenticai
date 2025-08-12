using Azure.AI.Agents.Persistent;

namespace HospitalSchedulingApp.Agent.MetaResolver
{
    public static class ResolveEntitiesTool
    {
        public static FunctionToolDefinition GetTool()
        {
            return new FunctionToolDefinition(
                name: "resolveEntities",
                description: "Unified entity resolver tool that extracts multiple entities — including department, staff, shift type, shift status, leave status, leave type,logged in user role, to resolve natural dates and more — from a single input phrase. Use this tool instead of calling individual entity resolvers separately to ensure consistent and centralized entity resolution.",
                parameters: BinaryData.FromObjectAsJson(new
                {
                    type = "object",
                    properties = new
                    {
                        phrase = new { type = "string", description = "Input phrase to resolve entities from" }
                    },
                    required = new[] { "phrase" }
                })
            );
        }
    }

}
