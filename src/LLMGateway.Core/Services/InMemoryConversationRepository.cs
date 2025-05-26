using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Conversation;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace LLMGateway.Core.Services;

/// <summary>
/// In-memory implementation of IConversationRepository for when database is not available
/// </summary>
public class InMemoryConversationRepository : IConversationRepository
{
    private readonly ILogger<InMemoryConversationRepository> _logger;
    private readonly ConcurrentDictionary<string, Conversation> _conversations = new();
    private readonly ConcurrentDictionary<string, ConversationMessage> _messages = new();

    public InMemoryConversationRepository(ILogger<InMemoryConversationRepository> logger)
    {
        _logger = logger;
    }

    public Task<IEnumerable<Conversation>> GetAllAsync(string userId, bool includeArchived = false)
    {
        _logger.LogDebug("Getting all conversations for user {UserId}", userId);

        var conversations = _conversations.Values
            .Where(c => c.UserId == userId && (includeArchived || !c.IsArchived))
            .OrderByDescending(c => c.UpdatedAt);

        return Task.FromResult(conversations.AsEnumerable());
    }

    public Task<Conversation?> GetByIdAsync(string conversationId)
    {
        _logger.LogDebug("Getting conversation {ConversationId}", conversationId);

        _conversations.TryGetValue(conversationId, out var conversation);
        return Task.FromResult(conversation);
    }

    public Task<Conversation> CreateAsync(Conversation conversation)
    {
        _logger.LogDebug("Creating conversation {ConversationId}", conversation.Id);

        conversation.Id = conversation.Id ?? Guid.NewGuid().ToString();
        conversation.CreatedAt = DateTime.UtcNow;
        conversation.UpdatedAt = DateTime.UtcNow;

        _conversations[conversation.Id] = conversation;

        return Task.FromResult(conversation);
    }

    public Task<Conversation> UpdateAsync(Conversation conversation)
    {
        _logger.LogDebug("Updating conversation {ConversationId}", conversation.Id);

        if (!_conversations.ContainsKey(conversation.Id))
        {
            throw new KeyNotFoundException($"Conversation {conversation.Id} not found");
        }

        conversation.UpdatedAt = DateTime.UtcNow;
        _conversations[conversation.Id] = conversation;

        return Task.FromResult(conversation);
    }

    public Task DeleteAsync(string conversationId)
    {
        _logger.LogDebug("Deleting conversation {ConversationId}", conversationId);

        _conversations.TryRemove(conversationId, out _);

        // Also remove associated messages
        var messagesToRemove = _messages.Values
            .Where(m => m.ConversationId == conversationId)
            .Select(m => m.Id)
            .ToList();

        foreach (var messageId in messagesToRemove)
        {
            _messages.TryRemove(messageId, out _);
        }

        return Task.CompletedTask;
    }

    public Task<ConversationMessage> AddMessageAsync(ConversationMessage message)
    {
        _logger.LogDebug("Adding message {MessageId} to conversation {ConversationId}", message.Id, message.ConversationId);

        message.Id = message.Id ?? Guid.NewGuid().ToString();
        message.CreatedAt = DateTime.UtcNow;

        _messages[message.Id] = message;

        // Update conversation's last updated time
        if (_conversations.TryGetValue(message.ConversationId, out var conversation))
        {
            conversation.UpdatedAt = DateTime.UtcNow;
            _conversations[message.ConversationId] = conversation;
        }

        return Task.FromResult(message);
    }

    public Task<IEnumerable<ConversationMessage>> GetMessagesAsync(string conversationId)
    {
        _logger.LogDebug("Getting messages for conversation {ConversationId}", conversationId);

        var messages = _messages.Values
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt);

        return Task.FromResult(messages.AsEnumerable());
    }

    public Task<ConversationMessage?> GetMessageByIdAsync(string messageId)
    {
        _logger.LogDebug("Getting message {MessageId}", messageId);

        _messages.TryGetValue(messageId, out var message);
        return Task.FromResult(message);
    }

    public Task DeleteMessageAsync(string messageId)
    {
        _logger.LogDebug("Deleting message {MessageId}", messageId);

        _messages.TryRemove(messageId, out _);
        return Task.CompletedTask;
    }

    public Task ArchiveAsync(string conversationId)
    {
        _logger.LogDebug("Archiving conversation {ConversationId}", conversationId);

        if (_conversations.TryGetValue(conversationId, out var conversation))
        {
            conversation.IsArchived = true;
            conversation.UpdatedAt = DateTime.UtcNow;
            _conversations[conversationId] = conversation;
        }

        return Task.CompletedTask;
    }

    public Task<(IEnumerable<Conversation> Conversations, int TotalCount)> SearchAsync(
        string userId,
        string? query,
        IEnumerable<string>? tags,
        bool includeArchived,
        DateTime? startDate,
        DateTime? endDate,
        int page,
        int pageSize)
    {
        _logger.LogDebug("Searching conversations for user {UserId} with query: {Query}", userId, query);

        var queryable = _conversations.Values
            .Where(c => c.UserId == userId && (includeArchived || !c.IsArchived))
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(query))
        {
            queryable = queryable.Where(c =>
                c.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (c.SystemPrompt != null && c.SystemPrompt.Contains(query, StringComparison.OrdinalIgnoreCase)));
        }

        if (tags != null && tags.Any())
        {
            var tagList = tags.ToList();
            queryable = queryable.Where(c => c.Tags.Any(tag => tagList.Contains(tag)));
        }

        if (startDate.HasValue)
        {
            queryable = queryable.Where(c => c.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            queryable = queryable.Where(c => c.CreatedAt <= endDate.Value);
        }

        var totalCount = queryable.Count();

        var conversations = queryable
            .OrderByDescending(c => c.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult((conversations.AsEnumerable(), totalCount));
    }
}
