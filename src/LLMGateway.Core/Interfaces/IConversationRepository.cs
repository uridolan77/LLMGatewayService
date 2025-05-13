using LLMGateway.Core.Models.Conversation;

namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Interface for conversation repository
/// </summary>
public interface IConversationRepository
{
    /// <summary>
    /// Get all conversations for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="includeArchived">Whether to include archived conversations</param>
    /// <returns>List of conversations</returns>
    Task<IEnumerable<Conversation>> GetAllAsync(string userId, bool includeArchived = false);
    
    /// <summary>
    /// Get conversation by ID
    /// </summary>
    /// <param name="conversationId">Conversation ID</param>
    /// <returns>Conversation</returns>
    Task<Conversation?> GetByIdAsync(string conversationId);
    
    /// <summary>
    /// Create conversation
    /// </summary>
    /// <param name="conversation">Conversation to create</param>
    /// <returns>Created conversation</returns>
    Task<Conversation> CreateAsync(Conversation conversation);
    
    /// <summary>
    /// Update conversation
    /// </summary>
    /// <param name="conversation">Conversation to update</param>
    /// <returns>Updated conversation</returns>
    Task<Conversation> UpdateAsync(Conversation conversation);
    
    /// <summary>
    /// Delete conversation
    /// </summary>
    /// <param name="conversationId">Conversation ID</param>
    /// <returns>Task</returns>
    Task DeleteAsync(string conversationId);
    
    /// <summary>
    /// Add message to conversation
    /// </summary>
    /// <param name="message">Message to add</param>
    /// <returns>Added message</returns>
    Task<ConversationMessage> AddMessageAsync(ConversationMessage message);
    
    /// <summary>
    /// Get conversation messages
    /// </summary>
    /// <param name="conversationId">Conversation ID</param>
    /// <returns>List of messages</returns>
    Task<IEnumerable<ConversationMessage>> GetMessagesAsync(string conversationId);
    
    /// <summary>
    /// Get message by ID
    /// </summary>
    /// <param name="messageId">Message ID</param>
    /// <returns>Message</returns>
    Task<ConversationMessage?> GetMessageByIdAsync(string messageId);
    
    /// <summary>
    /// Delete message
    /// </summary>
    /// <param name="messageId">Message ID</param>
    /// <returns>Task</returns>
    Task DeleteMessageAsync(string messageId);
    
    /// <summary>
    /// Search conversations
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="query">Search query</param>
    /// <param name="tags">Filter by tags</param>
    /// <param name="includeArchived">Include archived conversations</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Search results</returns>
    Task<(IEnumerable<Conversation> Conversations, int TotalCount)> SearchAsync(
        string userId,
        string? query,
        IEnumerable<string>? tags,
        bool includeArchived,
        DateTime? startDate,
        DateTime? endDate,
        int page,
        int pageSize);
}
