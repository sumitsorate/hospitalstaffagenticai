using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.Handlers;
using HospitalSchedulingApp.Dal.Entities;
using HospitalSchedulingApp.Dtos.Auth;
using HospitalSchedulingApp.Services.AuthServices.Interfaces;
using HospitalSchedulingApp.Services.Interfaces;
using System.Text.Json;
using System.Threading;

namespace HospitalSchedulingApp.Agent.Services
{
    /// <summary>
    /// Service responsible for handling interactions with the Persistent Agent,
    /// including sending messages, receiving responses, and resolving tool calls.
    /// </summary>
    public class AgentService : IAgentService
    {
        private readonly PersistentAgentsClient _client;
        private readonly PersistentAgent _agent;
        private readonly ILogger<AgentService> _logger;
        private readonly IEnumerable<IToolHandler> _toolHandlers;
        private readonly IAgentConversationService _agentConversationService;
        private readonly IUserContextService _userContextService;
        private readonly IAgentManager _agentManager;

        public AgentService(
            PersistentAgentsClient persistentAgentsClient,

            PersistentAgent agent,
            IEnumerable<IToolHandler> toolHandlers,
            IAgentConversationService agentConversationService,
            IUserContextService userContextService,
            ILogger<AgentService> logger
            )
        {
            _client = persistentAgentsClient;
            _agent = agent;
            _toolHandlers = toolHandlers;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _agentConversationService = agentConversationService;
            _userContextService = userContextService;

        }

        /// <summary>
        /// Creates a new persistent agent thread for communication.
        /// </summary>
        public async Task<string> Refresh()
        {
            // Delete thread for currently logged in user
            await DeleteThreadForUserAsync();
            // Create a new thread for the user
            var staffId = _userContextService.GetStaffId();

            // Create new thread for user
            var threadId = await FetchOrCreateThreadForUser(staffId);

            return threadId;

        }
        /// <summary>
        /// Creates a new persistent agent thread for communication.
        /// </summary>
        public async Task<PersistentAgentThread> CreateThreadAsync()
        {
            try
            {
                _logger.LogInformation("Creating new persistent agent thread...");
                var thread = await _client.Threads.CreateThreadAsync();
                _logger.LogInformation("Successfully created thread with ID: {ThreadId}", thread.Value.Id);
                return thread;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating new persistent agent thread.");
                throw;
            }
        }

        /// <summary>
        /// Fetches an existing thread for the user or creates a new one if none exists.
        /// </summary>
        public async Task<string> FetchOrCreateThreadForUser(int? staffId = null)
        {
            try
            {
                // Try to resolve staffId from context if not passed
                if (staffId == null || staffId <= 0)
                {
                    var contextStaffId = _userContextService.GetStaffId();
                    if (contextStaffId > 0)
                        staffId = contextStaffId;
                }

                // If staffId is known, check for existing thread
                if (staffId.HasValue && staffId > 0)
                {
                    var existingThreadId = await _agentConversationService.FetchThreadIdForLoggedInUser(staffId.Value);
                    if (!string.IsNullOrEmpty(existingThreadId))
                    {
                        _logger.LogInformation("Existing thread found for StaffId {StaffId}: {ThreadId}", staffId, existingThreadId);
                        return existingThreadId;
                    }
                }

                // No thread found — create a new one
                var newThread = await CreateThreadAsync();

                // If we have staffId, store the conversation
                if (staffId.HasValue && staffId > 0)
                {
                    var agentConversation = new AgentConversations
                    {
                        UserId = staffId.Value.ToString(), // since UserId == StaffId
                        ThreadId = newThread.Id,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _agentConversationService.AddAgentConversation(agentConversation);
                    _logger.LogInformation("New thread {ThreadId} created and saved for StaffId {StaffId}", newThread.Id, staffId);
                }
                else
                {
                    _logger.LogInformation("New thread {ThreadId} created for unauthenticated user (no staffId yet)", newThread.Id);
                }

                return newThread.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch or create thread for user.");
                throw;
            }
        }

        /// <summary>
        /// Deletes a thread from OpenAI and your system.
        /// </summary>
        public async Task DeleteThreadForUserAsync()
        {
            var staffId = _userContextService.GetStaffId();
            var threadId = await _agentConversationService.FetchThreadIdForLoggedInUser(staffId);
            if (string.IsNullOrWhiteSpace(threadId))
            {
                _logger.LogWarning("ThreadId is null or empty. Skipping thread deletion.");
                return;
            }

            try
            {
                _logger.LogInformation("Deleting thread with ID: {ThreadId}", threadId);
                await _client.Threads.DeleteThreadAsync(threadId);
                _logger.LogInformation("Successfully deleted thread from OpenAI: {ThreadId}", threadId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while deleting thread from OpenAI: {ThreadId}", threadId);
            }

            try
            {
                var agentConversation = await _agentConversationService.FetchLoggedInUserAgentConversationInfo();
                if (agentConversation != null)
                {
                    await _agentConversationService.DeleteAgentConversation(agentConversation);
                    _logger.LogInformation("Deleted agent conversation entry for ThreadId: {ThreadId}", threadId);
                }
                else
                {
                    _logger.LogInformation("No agent conversation found to delete for ThreadId: {ThreadId}", threadId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while deleting agent conversation for thread {ThreadId}", threadId);
                throw;
            }
        }

        /// <summary>
        /// Adds a user message to the provided thread.
        /// </summary>
        public async Task AddUserMessageAsync(string threadId, MessageRole role, string message)
        {
            _logger.LogInformation($"Adding user message to thread {threadId}: {message}");
            await _client.Messages.CreateMessageAsync(threadId, MessageRole.User, message);

        }

        public async Task<MessageContent?> GetAgentResponseAsync(MessageRole role, string message)
        {
            int maxRetries = 5;
            int retryDelaySeconds = 3;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var threadId = await FetchOrCreateThreadForUser();
                    await _client.Messages.CreateMessageAsync(threadId, MessageRole.User, message);

                    ThreadRun run = await _client.Runs.CreateRunAsync(threadId, _agent.Id);

                    do
                    {
                        await Task.Delay(500);
                        run = await _client.Runs.GetRunAsync(threadId, run.Id);

                        if (run.Status == RunStatus.RequiresAction &&
                            run.RequiredAction is SubmitToolOutputsAction action)
                        {
                            var toolOutputs = new List<ToolOutput>();
                            foreach (var toolCall in action.ToolCalls)
                            {
                                var result = await GetResolvedToolOutputAsync(toolCall);
                                if (result != null)
                                    toolOutputs.Add(result);
                            }
                            run = await _client.Runs.SubmitToolOutputsToRunAsync(threadId, run.Id, toolOutputs);
                        }

                        // Check if run failed
                        if (run.Status == RunStatus.Failed && run.LastError != null)
                        {
                            if (!string.IsNullOrEmpty(run.LastError.Code) && run.LastError.Code.Contains("rate_limit_exceeded", StringComparison.OrdinalIgnoreCase))
                            {
                                _logger.LogWarning("Rate limit exceeded. Waiting {RetryDelaySeconds} seconds before retrying attempt {Attempt}/{MaxRetries}.", retryDelaySeconds, attempt, maxRetries);

                                await Task.Delay(retryDelaySeconds * 1000);

                                throw new Exception("Rate limit exceeded, retrying...");
                            }
                            else
                            {
                                _logger.LogError("Run failed with error code: {Code}, message: {Message}", run.LastError.Code, run.LastError.Message);
                                break; // Exit retry loop on other errors
                            }
                        }

                    }
                    while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress || run.Status == RunStatus.RequiresAction);

                    //var messages = _client.Messages.GetMessages(threadId, order: ListSortOrder.Descending);

                    //foreach (var msg in messages)
                    //{
                    //    var messageText = msg.ContentItems.OfType<MessageTextContent>().FirstOrDefault();
                    //    _logger.LogInformation("Returning message content: {Text}", messageText?.Text);
                    //    return messageText;
                    //}

                    //return null;

                    var messages = _client.Messages.GetMessages(threadId, runId: run.Id, order: ListSortOrder.Descending);
                    foreach (var msg in messages)
                    {
                        if (msg.Role == MessageRole.Agent)  // Only consider assistant/bot replies
                        {
                            var messageText = msg.ContentItems.OfType<MessageTextContent>().FirstOrDefault();
                            if (messageText != null)
                            {
                                _logger.LogInformation("Returning message content: {Text}", messageText.Text);
                                return messageText;
                            }
                        }
                    }

                    // If no assistant message found, return a fallback error message
                    _logger.LogWarning("No assistant response found in messages after run completion.");
                      
                }
                catch (Exception ex)
                {
                    if (attempt == maxRetries)
                    {
                        _logger.LogError(ex, "Failed after {MaxRetries} attempts due to rate limit or other errors.", maxRetries);
                        throw; // rethrow after max retries
                    }

                    _logger.LogWarning(ex, "Attempt {Attempt} failed, retrying...", attempt);
                    // will retry on next loop iteration
                }
            }

            return null; // fallback, should never reach here
        }


        /// <summary>
        /// Resolves a single tool call by matching it with a registered IToolHandler.
        /// </summary>
        public async Task<ToolOutput?> GetResolvedToolOutputAsync(RequiredToolCall toolCall)
        {
            if (toolCall is not RequiredFunctionToolCall functionToolCall)
                return null;

            _logger.LogInformation("Tool invoked: {ToolName} | ID: {ToolId}", functionToolCall.Name, toolCall.Id);
            _logger.LogInformation("Arguments: {Arguments}", functionToolCall.Arguments);

            try
            {
                using var doc = JsonDocument.Parse(functionToolCall.Arguments);
                var root = doc.RootElement;

                var handler = _toolHandlers.FirstOrDefault(h => h.ToolName == functionToolCall.Name);
                if (handler == null)
                {
                    _logger.LogWarning("No handler found for tool: {ToolName}", functionToolCall.Name);
                    return null;
                }

                return await handler.HandleAsync(functionToolCall, root);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while resolving tool output for {ToolName}", functionToolCall.Name);
                return null;
            }
        }

    }
}


//var threads =  _client.Threads.GetThreads();
//foreach (var item in threads)
//{
//    await _client.Threads.DeleteThreadAsync(item.Id);
//}

// _logger.LogInformation($"Thread {thread.Id} deleted.");
//var thread = CreateThread();
//threadId =  thread.Id;
