using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Conversation;
using LLMGateway.Infrastructure.Persistence;
using LLMGateway.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LLMGateway.Infrastructure.Repositories;

/// <summary>
/// Repository for conversations
/// </summary>
public class ConversationRepository : IConversationRepository
{
    private readonly LLMGatewayDbContext _dbContext;
    private readonly ILogger<ConversationRepository> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="dbContext">Database context</param>
    /// <param name="logger">Logger</param>
    public ConversationRepository(
        LLMGatewayDbContext dbContext,
        ILogger<ConversationRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Core.Models.Conversation.Conversation>> GetAllAsync(string userId, bool includeArchived = false)
    {
        try
        {
            var query = _dbContext.Conversations
                .Where(c => c.UserId == userId);

            if (!includeArchived)
            {
                query = query.Where(c => !c.IsArchived);
            }

            var entities = await query
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();

            return entities.Select(e => e.ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all conversations for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Core.Models.Conversation.Conversation?> GetByIdAsync(string conversationId)
    {
        try
        {
            var entity = await _dbContext.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            return entity?.ToDomainModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get conversation {ConversationId}", conversationId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Core.Models.Conversation.Conversation> CreateAsync(Core.Models.Conversation.Conversation conversation)
    {
        try
        {
            var entity = Persistence.Entities.Conversation.FromDomainModel(conversation);

            _dbContext.Conversations.Add(entity);
            await _dbContext.SaveChangesAsync();

            return entity.ToDomainModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create conversation {ConversationId}", conversation.Id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Core.Models.Conversation.Conversation> UpdateAsync(Core.Models.Conversation.Conversation conversation)
    {
        try
        {
            var entity = await _dbContext.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversation.Id);

            if (entity == null)
            {
                throw new Exception($"Conversation with ID {conversation.Id} not found");
            }

            // Update the entity
            entity.Title = conversation.Title;
            entity.UpdatedAt = conversation.UpdatedAt;
            entity.SetMetadata(conversation.Metadata);
            entity.ModelId = conversation.ModelId;
            entity.SystemPrompt = conversation.SystemPrompt;
            entity.IsArchived = conversation.IsArchived;
            entity.SetTags(conversation.Tags);

            _dbContext.Conversations.Update(entity);
            await _dbContext.SaveChangesAsync();

            return entity.ToDomainModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update conversation {ConversationId}", conversation.Id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string conversationId)
    {
        try
        {
            var entity = await _dbContext.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (entity != null)
            {
                _dbContext.Conversations.Remove(entity);
                await _dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete conversation {ConversationId}", conversationId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Core.Models.Conversation.ConversationMessage> AddMessageAsync(Core.Models.Conversation.ConversationMessage message)
    {
        try
        {
            var entity = Persistence.Entities.ConversationMessage.FromDomainModel(message);

            _dbContext.ConversationMessages.Add(entity);
            await _dbContext.SaveChangesAsync();

            return entity.ToDomainModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add message {MessageId} to conversation {ConversationId}",
                message.Id, message.ConversationId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Core.Models.Conversation.ConversationMessage>> GetMessagesAsync(string conversationId)
    {
        try
        {
            var entities = await _dbContext.ConversationMessages
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            return entities.Select(e => e.ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get messages for conversation {ConversationId}", conversationId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Core.Models.Conversation.ConversationMessage?> GetMessageByIdAsync(string messageId)
    {
        try
        {
            var entity = await _dbContext.ConversationMessages
                .FirstOrDefaultAsync(m => m.Id == messageId);

            return entity?.ToDomainModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get message {MessageId}", messageId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteMessageAsync(string messageId)
    {
        try
        {
            var entity = await _dbContext.ConversationMessages
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (entity != null)
            {
                _dbContext.ConversationMessages.Remove(entity);
                await _dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete message {MessageId}", messageId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<(IEnumerable<Core.Models.Conversation.Conversation> Conversations, int TotalCount)> SearchAsync(
        string userId,
        string? query,
        IEnumerable<string>? tags,
        bool includeArchived,
        DateTime? startDate,
        DateTime? endDate,
        int page,
        int pageSize)
    {
        try
        {
            var queryable = _dbContext.Conversations
                .Where(c => c.UserId == userId);

            if (!includeArchived)
            {
                queryable = queryable.Where(c => !c.IsArchived);
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                queryable = queryable.Where(c =>
                    c.Title.Contains(query) ||
                    c.SystemPrompt.Contains(query));
            }

            if (tags != null && tags.Any())
            {
                var tagList = tags.ToList();

                // This is a simple approach - in a real implementation, you would use a more sophisticated
                // approach to search for tags in the JSON array
                queryable = queryable.Where(c => tagList.Any(tag => c.TagsJson.Contains(tag)));
            }

            if (startDate.HasValue)
            {
                queryable = queryable.Where(c => c.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                queryable = queryable.Where(c => c.CreatedAt <= endDate.Value);
            }

            // Get total count
            var totalCount = await queryable.CountAsync();

            // Apply pagination
            var entities = await queryable
                .OrderByDescending(c => c.UpdatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (entities.Select(e => e.ToDomainModel()).ToList(), totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search conversations for user {UserId}", userId);
            throw;
        }
    }
}
