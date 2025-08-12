using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.Handlers;
using HospitalSchedulingApp.Agent.MetaResolver;
using HospitalSchedulingApp.Agent.Tools.Department;
using System.Text.Json;

public class ResolveEntitiesToolHandler : IToolHandler
{
    private readonly IEntityResolver _entityResolver;

    public ResolveEntitiesToolHandler(IEntityResolver entityResolver)
    {
        _entityResolver = entityResolver;
    }

    public string ToolName => ResolveEntitiesTool.GetTool().Name;
 
    public async Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root)
    {
        string phrase = root.TryGetProperty("phrase", out var phraseProp)
            ? phraseProp.GetString()?.Trim() ?? string.Empty
            : string.Empty;

        if (string.IsNullOrWhiteSpace(phrase))
        {
            return CreateError(call.Id, "Phrase is required for resolving entities.");
        }

        var resolved = await _entityResolver.ResolveEntitiesAsync(phrase);

        var result = new
        {
            success = true,
            resolved.Department,
            resolved.ShiftType,
            resolved.ShiftStatus,
            resolved.Staff,
            resolved.LeaveStatus,
            resolved.LeaveType,
            resolved.LoggedInUserRole,
            resolved.DateRange
        };

        return new ToolOutput(call.Id, JsonSerializer.Serialize(result));
    }

    private ToolOutput CreateError(string callId, string message)
    {
        var errorJson = JsonSerializer.Serialize(new { success = false, error = message });
        return new ToolOutput(callId, errorJson);
    }
}
