using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Common.Handlers.Interfaces;
using System.Text.Json;

namespace HospitalSchedulingApp.Common.Handlers
{
    public abstract class BaseToolHandler : IToolHandler
    {
        protected readonly ILogger _logger;

        protected BaseToolHandler(ILogger logger)
        {
            _logger = logger;
        }

        // Each handler must provide ToolName + HandleAsync
        public abstract string ToolName { get; }

        public abstract Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root);

        // 🔴 Common error response builder
        protected ToolOutput CreateError(string callId, string message)
        {
            var errorJson = JsonSerializer.Serialize(new
            {
                success = false,
                error = message
            });

            return new ToolOutput(callId, errorJson);
        }

        // 🟢 Common success response builder
        protected ToolOutput? CreateSuccess(string callId, string message, object? data = null)
        {
            var response = JsonSerializer.Serialize(new
            {
                success = true,
                message,
                data
            });

            return new ToolOutput(callId, response);
        }
    }
}

