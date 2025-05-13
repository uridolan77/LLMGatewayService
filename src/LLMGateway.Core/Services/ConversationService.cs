using LLMGateway.Core.Exceptions;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Completion;
using LLMGateway.Core.Models.Conversation;
using Microsoft.Extensions.Logging;

namespace LLMGateway.Core.Services;

/// <summary>
/// Service for managing conversations
/// </summary>
public class ConversationService : IConversationService
{
    private readonly IConversationRepository _repository;
    private readonly ICompletionService _completionService;
    private readonly ILogger<ConversationService> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="repository">Conversation repository</param>
    /// <param name="completionService">Completion service</param>
    /// <param name="logger">Logger</param>
    public ConversationService(
        IConversationRepository repository,
        ICompletionService completionService,
        ILogger<ConversationService> logger)
    {
        _repository = repository;
        _completionService = completionService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Conversation>> GetAllConversationsAsync(string userId, bool includeArchived = false)
    {
        try
        {
            return await _repository.GetAllAsync(userId, includeArchived);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all conversations for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Conversation> GetConversationAsync(string conversationId, string userId)
    {
        try
        {
            var conversation = await _repository.GetByIdAsync(conversationId);
            if (conversation == null)
            {
                throw new NotFoundException($"Conversation with ID {conversationId} not found");
            }

            // Check if the user has access to the conversation
            if (conversation.UserId != userId)
            {
                throw new ForbiddenException("You don't have access to this conversation");
            }

            // Load messages
            var messages = await _repository.GetMessagesAsync(conversationId);
            conversation.Messages = messages.ToList();

            return conversation;
        }
        catch (Exception ex) when (ex is not NotFoundException && ex is not ForbiddenException)
        {
            _logger.LogError(ex, "Failed to get conversation {ConversationId} for user {UserId}", conversationId, userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Conversation> CreateConversationAsync(CreateConversationRequest request, string userId)
    {
        try
        {
            // Validate the request
            if (string.IsNullOrWhiteSpace(request.ModelId))
            {
                throw new ValidationException("Model ID is required");
            }

            // Create the conversation
            var conversation = new Conversation
            {
                Id = Guid.NewGuid().ToString(),
                Title = request.Title ?? $"Conversation {DateTime.UtcNow:yyyy-MM-dd HH:mm}",
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ModelId = request.ModelId,
                SystemPrompt = request.SystemPrompt ?? "You are a helpful assistant.",
                Metadata = request.Metadata ?? new Dictionary<string, string>(),
                Tags = request.Tags ?? new List<string>()
            };

            // Create the conversation
            var createdConversation = await _repository.CreateAsync(conversation);

            // Add system message if system prompt is provided
            if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
            {
                var systemMessage = new ConversationMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    ConversationId = createdConversation.Id,
                    Role = "system",
                    Content = request.SystemPrompt,
                    CreatedAt = DateTime.UtcNow,
                    ModelId = request.ModelId,
                    Metadata = new Dictionary<string, string>()
                };

                await _repository.AddMessageAsync(systemMessage);
                createdConversation.Messages.Add(systemMessage);
            }

            // Add initial message if provided
            if (!string.IsNullOrWhiteSpace(request.InitialMessage))
            {
                var userMessage = new ConversationMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    ConversationId = createdConversation.Id,
                    Role = "user",
                    Content = request.InitialMessage,
                    CreatedAt = DateTime.UtcNow,
                    ModelId = request.ModelId,
                    Metadata = new Dictionary<string, string>()
                };

                await _repository.AddMessageAsync(userMessage);
                createdConversation.Messages.Add(userMessage);

                // Generate assistant response
                var continueRequest = new ContinueConversationRequest
                {
                    ConversationId = createdConversation.Id,
                    Message = request.InitialMessage,
                    ModelId = request.ModelId
                };

                await ContinueConversationAsync(continueRequest, userId);
            }

            return createdConversation;
        }
        catch (Exception ex) when (ex is not ValidationException)
        {
            _logger.LogError(ex, "Failed to create conversation for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Conversation> UpdateConversationAsync(string conversationId, UpdateConversationRequest request, string userId)
    {
        try
        {
            // Get the existing conversation
            var conversation = await GetConversationAsync(conversationId, userId);

            // Update the conversation properties
            if (request.Title != null)
            {
                conversation.Title = request.Title;
            }

            if (request.SystemPrompt != null)
            {
                conversation.SystemPrompt = request.SystemPrompt;

                // Update or add system message
                var systemMessage = conversation.Messages.FirstOrDefault(m => m.Role == "system");
                if (systemMessage != null)
                {
                    systemMessage.Content = request.SystemPrompt;
                    // Update the system message in the repository
                    // This would require an additional method in the repository
                }
                else
                {
                    // Add a new system message
                    var newSystemMessage = new ConversationMessage
                    {
                        Id = Guid.NewGuid().ToString(),
                        ConversationId = conversationId,
                        Role = "system",
                        Content = request.SystemPrompt,
                        CreatedAt = DateTime.UtcNow,
                        ModelId = conversation.ModelId,
                        Metadata = new Dictionary<string, string>()
                    };

                    await _repository.AddMessageAsync(newSystemMessage);
                    conversation.Messages.Add(newSystemMessage);
                }
            }

            if (request.Metadata != null)
            {
                conversation.Metadata = request.Metadata;
            }

            if (request.IsArchived.HasValue)
            {
                conversation.IsArchived = request.IsArchived.Value;
            }

            if (request.Tags != null)
            {
                conversation.Tags = request.Tags;
            }

            conversation.UpdatedAt = DateTime.UtcNow;

            return await _repository.UpdateAsync(conversation);
        }
        catch (Exception ex) when (ex is not NotFoundException && ex is not ForbiddenException)
        {
            _logger.LogError(ex, "Failed to update conversation {ConversationId} for user {UserId}", conversationId, userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteConversationAsync(string conversationId, string userId)
    {
        try
        {
            // Check if the conversation exists and the user has access
            var conversation = await GetConversationAsync(conversationId, userId);

            await _repository.DeleteAsync(conversationId);
        }
        catch (Exception ex) when (ex is not NotFoundException && ex is not ForbiddenException)
        {
            _logger.LogError(ex, "Failed to delete conversation {ConversationId} for user {UserId}", conversationId, userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ConversationMessage> AddMessageAsync(string conversationId, AddMessageRequest request, string userId)
    {
        try
        {
            // Check if the conversation exists and the user has access
            var conversation = await GetConversationAsync(conversationId, userId);

            // Validate the request
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                throw new ValidationException("Message content is required");
            }

            // Create the message
            var message = new ConversationMessage
            {
                Id = Guid.NewGuid().ToString(),
                ConversationId = conversationId,
                Role = request.Role,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow,
                ModelId = conversation.ModelId,
                Metadata = request.Metadata ?? new Dictionary<string, string>()
            };

            // Add the message
            var addedMessage = await _repository.AddMessageAsync(message);

            // Update the conversation's last update timestamp
            conversation.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(conversation);

            return addedMessage;
        }
        catch (Exception ex) when (ex is not NotFoundException && ex is not ForbiddenException && ex is not ValidationException)
        {
            _logger.LogError(ex, "Failed to add message to conversation {ConversationId} for user {UserId}", conversationId, userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<CompletionResponse> ContinueConversationAsync(ContinueConversationRequest request, string userId)
    {
        try
        {
            // Check if the conversation exists and the user has access
            var conversation = await GetConversationAsync(request.ConversationId, userId);

            // Validate the request
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                throw new ValidationException("Message content is required");
            }

            // Add the user message
            var userMessage = new ConversationMessage
            {
                Id = Guid.NewGuid().ToString(),
                ConversationId = request.ConversationId,
                Role = "user",
                Content = request.Message,
                CreatedAt = DateTime.UtcNow,
                ModelId = conversation.ModelId,
                Metadata = request.Metadata ?? new Dictionary<string, string>()
            };

            await _repository.AddMessageAsync(userMessage);

            // Prepare the completion request
            var messages = new List<Message>();

            // Add system message if available
            var systemMessage = conversation.Messages.FirstOrDefault(m => m.Role == "system");
            if (systemMessage != null)
            {
                messages.Add(new Message { Role = "system", Content = systemMessage.Content });
            }
            else if (!string.IsNullOrWhiteSpace(conversation.SystemPrompt))
            {
                messages.Add(new Message { Role = "system", Content = conversation.SystemPrompt });
            }

            // Add conversation history (excluding system messages)
            var historyMessages = conversation.Messages
                .Where(m => m.Role != "system")
                .OrderBy(m => m.CreatedAt)
                .ToList();

            // Add history messages
            foreach (var historyMessage in historyMessages)
            {
                messages.Add(new Message { Role = historyMessage.Role, Content = historyMessage.Content });
            }

            // Create the completion request
            var completionRequest = new CompletionRequest
            {
                ModelId = request.ModelId ?? conversation.ModelId,
                Messages = messages,
                Temperature = request.Temperature,
                MaxTokens = request.MaxTokens,
                User = userId
            };

            // Create the completion
            var completionResponse = await _completionService.CreateCompletionAsync(completionRequest);

            // Add the assistant message
            if (completionResponse.Choices.Count > 0 && completionResponse.Choices[0].Message != null)
            {
                var assistantMessage = new ConversationMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    ConversationId = request.ConversationId,
                    Role = "assistant",
                    Content = completionResponse.Choices[0].Message.Content ?? string.Empty,
                    CreatedAt = DateTime.UtcNow,
                    ModelId = completionResponse.Model,
                    Provider = completionResponse.Provider,
                    TokenUsage = completionResponse.Usage != null ? new MessageTokenUsage
                    {
                        PromptTokens = completionResponse.Usage.PromptTokens,
                        CompletionTokens = completionResponse.Usage.CompletionTokens,
                        TotalTokens = completionResponse.Usage.TotalTokens
                    } : null,
                    FunctionCall = completionResponse.Choices[0].Message.FunctionCall,
                    Metadata = new Dictionary<string, string>
                    {
                        ["finish_reason"] = completionResponse.Choices[0].FinishReason ?? string.Empty
                    }
                };

                await _repository.AddMessageAsync(assistantMessage);
            }

            // Update the conversation's last update timestamp
            conversation.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(conversation);

            return completionResponse;
        }
        catch (Exception ex) when (ex is not NotFoundException && ex is not ForbiddenException && ex is not ValidationException)
        {
            _logger.LogError(ex, "Failed to continue conversation {ConversationId} for user {UserId}", request.ConversationId, userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ConversationSearchResponse> SearchConversationsAsync(ConversationSearchRequest request, string userId)
    {
        try
        {
            var (conversations, totalCount) = await _repository.SearchAsync(
                userId,
                request.Query,
                request.Tags,
                request.IncludeArchived,
                request.StartDate,
                request.EndDate,
                request.Page,
                request.PageSize);

            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

            return new ConversationSearchResponse
            {
                Conversations = conversations.ToList(),
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = totalPages
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search conversations for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ConversationMessage>> GetConversationMessagesAsync(string conversationId, string userId)
    {
        try
        {
            // Check if the conversation exists and the user has access
            await GetConversationAsync(conversationId, userId);

            return await _repository.GetMessagesAsync(conversationId);
        }
        catch (Exception ex) when (ex is not NotFoundException && ex is not ForbiddenException)
        {
            _logger.LogError(ex, "Failed to get messages for conversation {ConversationId} for user {UserId}", conversationId, userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ConversationMessage> GetMessageAsync(string messageId, string userId)
    {
        try
        {
            var message = await _repository.GetMessageByIdAsync(messageId);
            if (message == null)
            {
                throw new NotFoundException($"Message with ID {messageId} not found");
            }

            // Check if the user has access to the conversation
            var conversation = await _repository.GetByIdAsync(message.ConversationId);
            if (conversation == null || conversation.UserId != userId)
            {
                throw new ForbiddenException("You don't have access to this message");
            }

            return message;
        }
        catch (Exception ex) when (ex is not NotFoundException && ex is not ForbiddenException)
        {
            _logger.LogError(ex, "Failed to get message {MessageId} for user {UserId}", messageId, userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteMessageAsync(string messageId, string userId)
    {
        try
        {
            // Check if the message exists and the user has access
            var message = await GetMessageAsync(messageId, userId);

            await _repository.DeleteMessageAsync(messageId);

            // Update the conversation's last update timestamp
            var conversation = await _repository.GetByIdAsync(message.ConversationId);
            if (conversation != null)
            {
                conversation.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(conversation);
            }
        }
        catch (Exception ex) when (ex is not NotFoundException && ex is not ForbiddenException)
        {
            _logger.LogError(ex, "Failed to delete message {MessageId} for user {UserId}", messageId, userId);
            throw;
        }
    }
}
