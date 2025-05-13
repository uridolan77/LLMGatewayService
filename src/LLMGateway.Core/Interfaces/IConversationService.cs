using LLMGateway.Core.Models.Completion;
using LLMGateway.Core.Models.Conversation;

namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Interface for conversation service
/// </summary>
public interface IConversationService
{
    /// <summary>
    /// Get all conversations for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="includeArchived">Whether to include archived conversations</param>
    /// <returns>List of conversations</returns>
    Task<IEnumerable<Conversation>> GetAllConversationsAsync(string userId, bool includeArchived = false);
    
    /// <summary>
    /// Get conversation by ID
    /// </summary>
    /// <param name="conversationId">Conversation ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Conversation</returns>
    Task<Conversation> GetConversationAsync(string conversationId, string userId);
    
    /// <summary>
    /// Create conversation
    /// </summary>
    /// <param name="request">Create request</param>
    /// <param name="userId">User ID</param>
    /// <returns>Created conversation</returns>
    Task<Conversation> CreateConversationAsync(CreateConversationRequest request, string userId);
    
    /// <summary>
    /// Update conversation
    /// </summary>
    /// <param name="conversationId">Conversation ID</param>
    /// <param name="request">Update request</param>
    /// <param name="userId">User ID</param>
    /// <returns>Updated conversation</returns>
    Task<Conversation> UpdateConversationAsync(string conversationId, UpdateConversationRequest request, string userId);
    
    /// <summary>
    /// Delete conversation
    /// </summary>
    /// <param name="conversationId">Conversation ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Task</returns>
    Task DeleteConversationAsync(string conversationId, string userId);
    
    /// <summary>
    /// Add message to conversation
    /// </summary>
    /// <param name="conversationId">Conversation ID</param>
    /// <param name="request">Add message request</param>
    /// <param name="userId">User ID</param>
    /// <returns>Added message</returns>
    Task<ConversationMessage> AddMessageAsync(string conversationId, AddMessageRequest request, string userId);
    
    /// <summary>
    /// Continue conversation
    /// </summary>
    /// <param name="request">Continue conversation request</param>
    /// <param name="userId">User ID</param>
    /// <returns>Completion response</returns>
    Task<CompletionResponse> ContinueConversationAsync(ContinueConversationRequest request, string userId);
    
    /// <summary>
    /// Search conversations
    /// </summary>
    /// <param name="request">Search request</param>
    /// <param name="userId">User ID</param>
    /// <returns>Search response</returns>
    Task<ConversationSearchResponse> SearchConversationsAsync(ConversationSearchRequest request, string userId);
    
    /// <summary>
    /// Get conversation messages
    /// </summary>
    /// <param name="conversationId">Conversation ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>List of messages</returns>
    Task<IEnumerable<ConversationMessage>> GetConversationMessagesAsync(string conversationId, string userId);
    
    /// <summary>
    /// Get message by ID
    /// </summary>
    /// <param name="messageId">Message ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Message</returns>
    Task<ConversationMessage> GetMessageAsync(string messageId, string userId);
    
    /// <summary>
    /// Delete message
    /// </summary>
    /// <param name="messageId">Message ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Task</returns>
    Task DeleteMessageAsync(string messageId, string userId);
}
