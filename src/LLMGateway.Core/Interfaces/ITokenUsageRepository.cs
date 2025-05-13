using LLMGateway.Core.Models.TokenUsage;

namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Repository for token usage records
/// </summary>
public interface ITokenUsageRepository
{
    /// <summary>
    /// Add a token usage record
    /// </summary>
    /// <param name="record">Token usage record</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task AddAsync(TokenUsageRecord record, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get token usage records for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Token usage records</returns>
    Task<IEnumerable<TokenUsageRecord>> GetForUserAsync(string userId, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get token usage records for an API key
    /// </summary>
    /// <param name="apiKeyId">API key ID</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Token usage records</returns>
    Task<IEnumerable<TokenUsageRecord>> GetForApiKeyAsync(string apiKeyId, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get token usage records for a model
    /// </summary>
    /// <param name="modelId">Model ID</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Token usage records</returns>
    Task<IEnumerable<TokenUsageRecord>> GetForModelAsync(string modelId, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get token usage records for a provider
    /// </summary>
    /// <param name="provider">Provider name</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Token usage records</returns>
    Task<IEnumerable<TokenUsageRecord>> GetForProviderAsync(string provider, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all token usage records
    /// </summary>
    /// <param name="predicate">Filter predicate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Token usage records</returns>
    Task<IEnumerable<TokenUsageRecord>> GetAllAsync(Func<TokenUsageRecord, bool>? predicate = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get total token usage
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Token usage records</returns>
    Task<IEnumerable<TokenUsageRecord>> GetTotalUsageAsync(DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default);
}
