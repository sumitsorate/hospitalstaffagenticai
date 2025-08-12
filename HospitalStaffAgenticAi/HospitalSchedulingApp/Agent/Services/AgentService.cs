﻿using Azure;
using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.Handlers;
using HospitalSchedulingApp.Dal.Entities;
using HospitalSchedulingApp.Dtos.Auth;
using HospitalSchedulingApp.Services.AuthServices.Interfaces;
using HospitalSchedulingApp.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
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
        private int _apiCallCount = 0;

        private async Task<T> CallAzureApiAsync<T>(Func<Task<T>> apiCall, string apiName, string? details = null)
        {
            _apiCallCount++;
            _logger.LogInformation("Azure API call #{Count} - {ApiName} {Details}", _apiCallCount, apiName, details ?? "");

            var stopwatch = Stopwatch.StartNew();
            var result = await apiCall();
            stopwatch.Stop();

            _logger.LogInformation("{ApiName} completed in {ElapsedMilliseconds} ms", apiName, stopwatch.ElapsedMilliseconds);

            return result;
        }

        //public async Task<MessageContent?> GetAgentResponseAsync(MessageRole role, string message)
        //{
        //    const int maxRetries = 5;
        //    const int initialPollDelayMs = 5000;  // Start polling every 5s
        //    const int maxPollDelayMs = 30000;     // Max 30s between polls
        //    const int initialRetryDelayMs = 5000; // Start retry after 5s when rate limit
        //    int retryDelayMs = initialRetryDelayMs;

        //    var threadId = await FetchOrCreateThreadForUser();

        //    _logger.LogInformation("Sending user message of length {Length} characters", message.Length);

        //    // Add user message to thread
        //    await CallAzureApiAsync(
        //        () => _client.Messages.CreateMessageAsync(threadId, MessageRole.User, message),
        //        "CreateMessageAsync",
        //        $"message length={message.Length}");

        //    int attempt = 0;
        //    ThreadRun run = await CallAzureApiAsync(
        //        () => _client.Runs.CreateRunAsync(threadId, _agent.Id),
        //        "CreateRunAsync",
        //        $"agentId={_agent.Id}");

        //    while (attempt < maxRetries)
        //    {
        //        attempt++;
        //        try
        //        {
        //            bool continuePolling;
        //            int pollDelay = initialPollDelayMs;

        //            do
        //            {
        //                await Task.Delay(pollDelay);

        //                run = await CallAzureApiAsync(
        //                    () => _client.Runs.GetRunAsync(threadId, run.Id),
        //                    "GetRunAsync",
        //                    $"runId={run.Id}");

        //                // Adaptive polling logic
        //                if (run.Status == RunStatus.Queued)
        //                    pollDelay = Math.Min(pollDelay * 2, maxPollDelayMs);
        //                else if (run.Status == RunStatus.InProgress)
        //                    pollDelay = Math.Min(pollDelay + 2000, maxPollDelayMs);

        //                if (run.Status == RunStatus.RequiresAction && run.RequiredAction is SubmitToolOutputsAction action)
        //                {
        //                    var toolOutputs = new List<ToolOutput>();
        //                    foreach (var toolCall in action.ToolCalls)
        //                    {
        //                        var output = await GetResolvedToolOutputAsync(toolCall);
        //                        if (output != null)
        //                            toolOutputs.Add(output);
        //                    }

        //                    run = await CallAzureApiAsync(
        //                        () => _client.Runs.SubmitToolOutputsToRunAsync(threadId, run.Id, toolOutputs),
        //                        "SubmitToolOutputsToRunAsync",
        //                        $"runId={run.Id}, toolCalls={toolOutputs.Count}");
        //                }

        //                if (run.Status == RunStatus.Failed && run.LastError != null)
        //                {
        //                    //// Cancel run before deciding next step
        //                    //await CallAzureApiAsync(
        //                    //    () => _client.Runs.CancelRunAsync(threadId, run.Id),
        //                    //    "CancelRunAsync",
        //                    //    $"runId={run.Id}");

        //                    if (!string.IsNullOrEmpty(run.LastError.Code) &&
        //                        run.LastError.Code.Contains("rate_limit_exceeded", StringComparison.OrdinalIgnoreCase))
        //                    {
        //                        throw new RateLimitExceededException("Rate limit exceeded.");
        //                    }
        //                    else
        //                    {
        //                        _logger.LogError("Run failed with error code: {Code}, message: {Message}", run.LastError.Code, run.LastError.Message);
        //                        return null; // Unrecoverable error
        //                    }
        //                }

        //                continuePolling = run.Status == RunStatus.Queued
        //                                 || run.Status == RunStatus.InProgress
        //                                 || run.Status == RunStatus.RequiresAction;

        //            } while (continuePolling);

        //            // Fetch messages after successful run completion
        //            var messages = await CallAzureApiAsync(
        //                () => Task.FromResult(_client.Messages.GetMessages(threadId, runId: run.Id, order: ListSortOrder.Descending)),
        //                "GetMessages",
        //                $"runId={run.Id}");

        //            foreach (var msg in messages)
        //            {
        //                if (msg.Role == MessageRole.Agent)
        //                {
        //                    var messageText = msg.ContentItems.OfType<MessageTextContent>().FirstOrDefault();
        //                    if (messageText != null)
        //                    {
        //                        _logger.LogInformation("Returning message content: {Text}", messageText.Text);
        //                        return messageText;
        //                    }
        //                }
        //            }

        //            _logger.LogWarning("No assistant response found after run completion.");
        //            return null;
        //        }
        //        catch (RateLimitExceededException ex)
        //        {
        //            if (attempt == maxRetries)
        //            {
        //                _logger.LogError(ex, "Max retries reached due to rate limit.");
        //                throw;
        //            }

        //            _logger.LogWarning(ex, "Rate limit hit on attempt {Attempt}, retrying after {RetryDelay}ms...", attempt, retryDelayMs);
        //            await Task.Delay(retryDelayMs);

        //            retryDelayMs = Math.Min(retryDelayMs * 2, 15000); // Exponential backoff for retries

        //            // Cancel current run before retrying
        //            try
        //            {
        //                await CallAzureApiAsync(
        //                    () => _client.Runs.CancelRunAsync(threadId, run.Id),
        //                    "CancelRunAsync",
        //                    $"runId={run.Id}");
        //            }
        //            catch (Exception cancelEx)
        //            {
        //                _logger.LogWarning(cancelEx, "Failed to cancel run {RunId} before retry", run.Id);
        //            }

        //            // Create a fresh run for retry
        //            run = await CallAzureApiAsync(
        //                () => _client.Runs.CreateRunAsync(threadId, _agent.Id),
        //                "CreateRunAsync",
        //                $"agentId={_agent.Id}");
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, "Run failed with unrecoverable error.");
        //            return null;
        //        }
        //    }

        //    return null;
        //}

        public async Task<MessageContent?> GetAgentResponseAsync(MessageRole role, string message)
        {
            const int maxRetries = 6;
            const int baseDelayMs = 10000;
            const int maxDelayMs = 60000;
            //const int retryDelay = 5000; // fixed 5 seconds delay, no exponential backoff

            var threadId = await FetchOrCreateThreadForUser();

            _logger.LogInformation("Sending user message of length {Length} characters", message.Length);

            // Add user message to thread
            await CallAzureApiAsync(
                () => _client.Messages.CreateMessageAsync(threadId, MessageRole.User, message),
                "CreateMessageAsync",
                $"message length={message.Length}");

            int attempt = 0;
            int delayMs = baseDelayMs;
            ThreadRun run = await CallAzureApiAsync(
                () => _client.Runs.CreateRunAsync(threadId, _agent.Id),
                "CreateRunAsync",
                $"agentId={_agent.Id}");

            while (attempt < maxRetries)
            {
                attempt++;
                try
                {
                    bool continuePolling;
                    do
                    {
                        await Task.Delay(delayMs);

                        run = await CallAzureApiAsync(
                            () => _client.Runs.GetRunAsync(threadId, run.Id),
                            "GetRunAsync",
                            $"runId={run.Id}");

                        // Increase delay exponentially with cap
                        delayMs = Math.Min(delayMs * 2, maxDelayMs);

                        if (run.Status == RunStatus.RequiresAction && run.RequiredAction is SubmitToolOutputsAction action)
                        {
                            var toolOutputs = new List<ToolOutput>();
                            foreach (var toolCall in action.ToolCalls)
                            {
                                var output = await GetResolvedToolOutputAsync(toolCall);
                                if (output != null)
                                    toolOutputs.Add(output);
                            }

                            run = await CallAzureApiAsync(
                                () => _client.Runs.SubmitToolOutputsToRunAsync(threadId, run.Id, toolOutputs),
                                "SubmitToolOutputsToRunAsync",
                                $"runId={run.Id}, toolCalls={toolOutputs.Count}");
                        }

                        if (run.Status == RunStatus.Failed && run.LastError != null)
                        {
                            await _client.Runs.CancelRunAsync(threadId, run.Id);
                            if (!string.IsNullOrEmpty(run.LastError.Code) &&
                                run.LastError.Code.Contains("rate_limit_exceeded", StringComparison.OrdinalIgnoreCase))
                            {
                                // Rate limit hit, break to retry loop
                                throw new RateLimitExceededException("Rate limit exceeded.");
                            }
                            else
                            {
                                _logger.LogError("Run failed with error code: {Code}, message: {Message}", run.LastError.Code, run.LastError.Message);
                                return null; // Unrecoverable error, exit
                            }
                        }

                        continuePolling = run.Status == RunStatus.Queued
                                         || run.Status == RunStatus.InProgress
                                         || run.Status == RunStatus.RequiresAction;

                    } while (continuePolling);

                    // Fetch messages after successful run completion
                    var messages = await CallAzureApiAsync(
                        () => Task.FromResult(_client.Messages.GetMessages(threadId, runId: run.Id, order: ListSortOrder.Descending)),
                        "GetMessages",
                        $"runId={run.Id}");

                    foreach (var msg in messages)
                    {
                        if (msg.Role == MessageRole.Agent)
                        {
                            var messageText = msg.ContentItems.OfType<MessageTextContent>().FirstOrDefault();
                            if (messageText != null)
                            {
                                _logger.LogInformation("Returning message content: {Text}", messageText.Text);
                                return messageText;
                            }
                        }
                    }

                    _logger.LogWarning("No assistant response found after run completion.");
                    return null;
                }
                catch (RateLimitExceededException ex)
                {
                    if (attempt == maxRetries)
                    {
                        _logger.LogError(ex, "Max retries reached due to rate limit.");
                        throw;
                    }

                    var jitter = Random.Shared.Next(500, 1500);
                    var retryDelay = Math.Min(delayMs * attempt, maxDelayMs) + jitter;

                    _logger.LogWarning(ex, "Rate limit hit on attempt {Attempt}, retrying after {RetryDelay}ms...", attempt, retryDelay);
                    await Task.Delay(retryDelay);

                    // Create a fresh run for retry
                    run = await CallAzureApiAsync(
                        () => _client.Runs.CreateRunAsync(threadId, _agent.Id),
                        "CreateRunAsync",
                        $"agentId={_agent.Id}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Run failed with unrecoverable error.");
                    return null;
                }
            }

            return null;
        }

        // Custom exception for clarity
        public class RateLimitExceededException : Exception
        {
            public RateLimitExceededException(string message) : base(message) { }
        }


        //public async Task<MessageContent?> GetAgentResponseAsync(MessageRole role, string message)
        //{
        //    int maxRetries = 5;
        //    int baseDelayMs = 5000;
        //    int maxDelayMs = 25000;

        //    var threadId = await FetchOrCreateThreadForUser();

        //    _logger.LogInformation("Sending user message of length {Length} characters", message.Length);

        //    // Create message and run once outside retry loop
        //    await CallAzureApiAsync(
        //        () => _client.Messages.CreateMessageAsync(threadId, MessageRole.User, message),
        //        "CreateMessageAsync",
        //        $"message length={message.Length}");

        //    ThreadRun run = await CallAzureApiAsync(
        //        () => _client.Runs.CreateRunAsync(threadId, _agent.Id),
        //        "CreateRunAsync",
        //        $"agentId={_agent.Id}");

        //    int delayMs = baseDelayMs;

        //    for (int attempt = 1; attempt <= maxRetries; attempt++)
        //    {
        //        try
        //        {
        //            do
        //            {
        //                await Task.Delay(delayMs);

        //                run = await CallAzureApiAsync(
        //                    () => _client.Runs.GetRunAsync(threadId, run.Id),
        //                    "GetRunAsync",
        //                    $"runId={run.Id}");

        //                // Increase delay exponentially with max cap
        //                delayMs = Math.Min(delayMs * 2, maxDelayMs);

        //                if (run.Status == RunStatus.RequiresAction &&
        //                    run.RequiredAction is SubmitToolOutputsAction action)
        //                {
        //                    var tasks = action.ToolCalls.Select(tc => GetResolvedToolOutputAsync(tc));
        //                    var results = await Task.WhenAll(tasks);
        //                    var toolOutputs = results.Where(r => r != null).ToList();

        //                    run = await CallAzureApiAsync(
        //                        () => _client.Runs.SubmitToolOutputsToRunAsync(threadId, run.Id, toolOutputs),
        //                        "SubmitToolOutputsToRunAsync",
        //                        $"runId={run.Id}, toolCalls={toolOutputs.Count}");
        //                }

        //                if (run.Status == RunStatus.Failed && run.LastError != null)
        //                {
        //                    if (!string.IsNullOrEmpty(run.LastError.Code) &&
        //                        run.LastError.Code.Contains("rate_limit_exceeded", StringComparison.OrdinalIgnoreCase))
        //                    {
        //                        // Log and throw to catch block for retry
        //                        _logger.LogWarning("Rate limit exceeded. Retrying attempt {Attempt}/{MaxRetries} after delay {Delay}ms.", attempt, maxRetries, delayMs);
        //                        throw new Exception("Rate limit exceeded, retrying...");
        //                    }
        //                    else
        //                    {
        //                        _logger.LogError("Run failed with error code: {Code}, message: {Message}", run.LastError.Code, run.LastError.Message);
        //                        // Exit retry loop on other errors
        //                        return null;
        //                    }
        //                }

        //            } while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress || run.Status == RunStatus.RequiresAction);

        //            var messages = await CallAzureApiAsync(
        //                () => Task.FromResult(_client.Messages.GetMessages(threadId, runId: run.Id, order: ListSortOrder.Descending)),
        //                "GetMessages",
        //                $"runId={run.Id}");

        //            foreach (var msg in messages)
        //            {
        //                if (msg.Role == MessageRole.Agent)
        //                {
        //                    var messageText = msg.ContentItems.OfType<MessageTextContent>().FirstOrDefault();
        //                    if (messageText != null)
        //                    {
        //                        _logger.LogInformation("Returning message content: {Text}", messageText.Text);
        //                        return messageText;
        //                    }
        //                }
        //            }

        //            _logger.LogWarning("No assistant response found in messages after run completion.");
        //            return null;
        //        }
        //        catch (Exception ex)
        //        {
        //            if (attempt == maxRetries)
        //            {
        //                _logger.LogError(ex, "Failed after {MaxRetries} attempts due to rate limit or other errors.", maxRetries);
        //                throw;
        //            }

        //            // Exponential backoff with jitter on retry delay
        //            var jitter = new Random().Next(500, 1500);
        //            var retryDelay = Math.Min(delayMs * attempt, maxDelayMs) + jitter;
        //            //var retryDelay = 3000;
        //            _logger.LogWarning(ex, "Attempt {Attempt} failed, retrying after {RetryDelay}ms...", attempt, retryDelay);

        //            await Task.Delay(retryDelay);
        //        }
        //    }

        //    return null;
        //}

    }
}