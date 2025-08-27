using Azure.AI.Agents.Persistent;
using System.Text.Json;

namespace HospitalSchedulingApp.Common.Handlers.Interfaces
{
    /// <summary>
    /// Defines a common interface for tool handlers used by the agent.
    /// A tool handler is responsible for processing a specific tool call
    /// (function invocation) received from the agent and returning an appropriate output.
    /// </summary>
    public interface IToolHandler
    {
        /// <summary>
        /// Gets the unique name of the tool that this handler is responsible for.
        /// This should match the tool name defined in the agent's configuration.
        /// </summary>
        string ToolName { get; }

        /// <summary>
        /// Handles the execution of the tool when invoked by the agent.
        /// </summary>
        /// <param name="call">
        /// The function tool call containing metadata and invocation details
        /// provided by the agent runtime.
        /// </param>
        /// <param name="root">
        /// A <see cref="JsonElement"/> representing the input arguments 
        /// passed to the tool by the agent.
        /// </param>
        /// <returns>
        /// A <see cref="Task{ToolOutput}"/> containing the tool's output
        /// or <c>null</c> if no output is produced.
        /// </returns>
        Task<ToolOutput?> HandleAsync(RequiredFunctionToolCall call, JsonElement root);
    }
}
