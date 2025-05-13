using LLMGateway.Infrastructure.Persistence.Entities;

namespace LLMGateway.Infrastructure.Persistence;

/// <summary>
/// Repository for token usage records
/// </summary>
public interface ITokenUsageRepository
{
    /// <summary>
    /// Add a token usage record
    /// </summary>
    /// <param name="record">Token usage record</param>
    /// <returns>Task</returns>
    Task AddAsync(TokenUsageRecord record);
    
    /// <summary>
    /// Get token usage records for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <returns>Token usage records</returns>
    Task<IEnumerable<TokenUsageRecord>> GetForUserAsync(string userId, DateTimeOffset startDate, DateTimeOffset endDate);
    
    /// <summary>
    /// Get token usage records for an API key
    /// </summary>
    /// <param name="apiKeyId">API key ID</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <returns>Token usage records</returns>
    Task<IEnumerable<TokenUsageRecord>> GetForApiKeyAsync(string apiKeyId, DateTimeOffset startDate, DateTimeOffset endDate);
    
    /// <summary>
    /// Get token usage records for a model
    /// </summary>
    /// <param name="modelId">Model ID</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <returns>Token usage records</returns>
    Task<IEnumerable<TokenUsageRecord>> GetForModelAsync(string modelId, DateTimeOffset startDate, DateTimeOffset endDate);
    
    /// <summary>
    /// Get token usage records for a provider
    /// </summary>
    /// <param name="provider">Provider</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <returns>Token usage records</returns>
    Task<IEnumerable<TokenUsageRecord>> GetForProviderAsync(string provider, DateTimeOffset startDate, DateTimeOffset endDate);
    
    /// <summary>
    /// Get all token usage records
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <returns>Token usage records</returns>
    Task<IEnumerable<TokenUsageRecord>> GetAllAsync(DateTimeOffset startDate, DateTimeOffset endDate);
    
    /// <summary>
    /// Delete token usage records older than a date
    /// </summary>
    /// <param name="date">Date</param>
    /// <returns>Number of records deleted</returns>
    Task<int> DeleteOlderThanAsync(DateTimeOffset date);
}
