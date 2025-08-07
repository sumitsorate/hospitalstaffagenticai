﻿using Azure.AI.Agents.Persistent;
using HospitalSchedulingApp.Agent.Handlers;
using HospitalSchedulingApp.Dal.Entities;
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

        public AgentService(
            PersistentAgentsClient persistentAgentsClient,
            PersistentAgent agent,
            IEnumerable<IToolHandler> toolHandlers,
            IAgentConversationService agentConversationService,
            IUserContextService userContextService,
            ILogger<AgentService> logger)
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
        public Task AddUserMessageAsync(string threadId, MessageRole role, string message)
        {
            _logger.LogInformation($"Adding user message to thread {threadId}: {message}");
            _client.Messages.CreateMessage(threadId, MessageRole.User, message);
            return Task.CompletedTask;
        }


        /// <summary>
        /// Sends a message to the agent and waits for its final response.
        /// </summary>
        /// 

        //public async Task<MessageContent?> GetAgentResponseAsync(MessageRole role, string message)
        //{
        //    try
        //    {
        //        var threadId = await FetchOrCreateThreadForUser();
        //        await AddUserMessageAsync(threadId, role, message);

        //        var run = await _client.Runs.CreateRunAsync(threadId, _agent.Id);

        //        while ( run.Value.Status == RunStatus.Queued ||
        //               run.Value.Status == RunStatus.InProgress ||
        //               run.Value.Status == RunStatus.RequiresAction)
        //        {
        //            // 🛠️ Handle tool invocation if needed
        //            if (run.Value.Status == RunStatus.RequiresAction &&
        //                run.Value.RequiredAction is SubmitToolOutputsAction action)
        //            {
        //                var toolOutputs = new List<ToolOutput>();

        //                foreach (var toolCall in action.ToolCalls)
        //                {
        //                    var output = await GetResolvedToolOutputAsync(toolCall);
        //                    if (output != null)
        //                        toolOutputs.Add(output);
        //                }

        //                run = await _client.Runs.SubmitToolOutputsToRunAsync(threadId, run.Value.Id, toolOutputs);
        //            }

        //            // ⏳ Delay before polling again
        //            await Task.Delay(500);

        //            // 🔄 Poll status again
        //            run = await _client.Runs.GetRunAsync(threadId, run.Value.Id);
        //        }

        //        // ✅ Run is complete, fetch assistant messages
        //        var messages = new List<PersistentThreadMessage>();

        //        await foreach (var msg in _client.Messages.GetMessagesAsync(
        //            threadId: threadId,
        //            //runId: run.Value.Id,
        //            order: ListSortOrder.Descending))
        //        {
        //            messages.Add(msg);
        //        }

        //        foreach (var msg in messages)
        //        {
        //            var messageText = msg.ContentItems.OfType<MessageTextContent>().FirstOrDefault();
        //            if (messageText != null)
        //            {
        //                _logger.LogInformation("Assistant says: {Text}", messageText.Text);
        //                return messageText;
        //            }
        //        }

        //        _logger.LogWarning("No assistant response found.");
        //        return null;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error in GetAgentResponseAsync");
        //        throw;
        //    }
        //}

        //Checking
        public async Task<MessageContent?> GetAgentResponseAsync(MessageRole role, string message)
        {
            try
            {
                var threadId = await FetchOrCreateThreadForUser();
                await AddUserMessageAsync(threadId, role, message);

                ThreadRun run = _client.Runs.CreateRun(threadId, _agent.Id);

                // 🔁 Loop until the run completes (initial phase)
                while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress || run.Status == RunStatus.RequiresAction)
                {
                    Thread.Sleep(500);
                    run = _client.Runs.GetRun(threadId, run.Id);

                    // 🛠️ Handle required tools
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

                        run = _client.Runs.SubmitToolOutputsToRun(threadId, run.Id, toolOutputs);

                        // 🔁 Continue waiting for final response after submitting tool outputs
                        while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress || run.Status == RunStatus.RequiresAction)
                        {
                            Thread.Sleep(500);
                            run = _client.Runs.GetRun(threadId, run.Id);

                            if (run.Status == RunStatus.RequiresAction &&
                                run.RequiredAction is SubmitToolOutputsAction followupAction)
                            {
                                var followupOutputs = new List<ToolOutput>();

                                foreach (var followupCall in followupAction.ToolCalls)
                                {
                                    var followupResult = await GetResolvedToolOutputAsync(followupCall);
                                    if (followupResult != null)
                                        followupOutputs.Add(followupResult);
                                }

                                run = _client.Runs.SubmitToolOutputsToRun(threadId, run.Id, followupOutputs);
                            }
                        }

                        break; // Exit outer loop once tool phase is completed
                    }
                }

                var messages = _client.Messages.GetMessages(
                    threadId: threadId,
                    order: ListSortOrder.Descending
                );

                foreach (var msg in messages)
                {
                    if(msg.Role == MessageRole.Agent)
                    {
                        var messageText = msg.ContentItems.OfType<MessageTextContent>().FirstOrDefault();
                        _logger.LogInformation(messageText?.Text);
                        return messageText;
                    }

                }

                return null;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAgentResponseAsync");
                throw;
            }
        }


        //public async Task<MessageContent?> GetAgentResponseAsync(MessageRole role, string message)
        //{
        //    try
        //    {
        //        var threadId = await FetchOrCreateThreadForUser();
        //        await AddUserMessageAsync(threadId, role, message);

        //        ThreadRun run = _client.Runs.CreateRun(threadId, _agent.Id);

        //        do
        //        {
        //            Thread.Sleep(500);
        //            run = _client.Runs.GetRun(threadId, run.Id);

        //            if (run.Status == RunStatus.RequiresAction &&
        //                run.RequiredAction is SubmitToolOutputsAction action)
        //            {
        //                var toolOutputs = new List<ToolOutput>();

        //                foreach (var toolCall in action.ToolCalls)
        //                {
        //                    var result = await GetResolvedToolOutputAsync(toolCall);
        //                    if (result != null)
        //                        toolOutputs.Add(result);
        //                }

        //                run = _client.Runs.SubmitToolOutputsToRun(threadId, run.Id, toolOutputs);
        //            }

        //        } while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress || run.Status == RunStatus.RequiresAction);

        //        var messages = _client.Messages.GetMessages(
        //            threadId: threadId,
        //            order: ListSortOrder.Descending
        //        );

        //        foreach (var msg in messages)
        //        {
        //            var messageText = msg.ContentItems.OfType<MessageTextContent>().FirstOrDefault();
        //            _logger.LogInformation(messageText?.Text);
        //            return messageText;
        //        }

        //        return null;
        //    }
        //    finally
        //    {
        //        //// Clean up thread
        //        //await _client.Threads.DeleteThreadAsync(thread.Id);
        //        //_logger.LogInformation($"Thread {thread.Id} deleted.");
        //    }
        //}

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
