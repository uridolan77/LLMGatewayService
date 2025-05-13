using LLMGateway.Core.Models.TokenUsage;

namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Service for tracking token usage
/// </summary>
public interface ITokenUsageService
{
    /// <summary>
    /// Track token usage
    /// </summary>
    /// <param name="record">Token usage record</param>
    /// <returns>Task</returns>
    Task TrackUsageAsync(TokenUsageRecord record);

    /// <summary>
    /// Track token usage for a completion request and response
    /// </summary>
    /// <param name="request">Completion request</param>
    /// <param name="response">Completion response</param>
    /// <returns>Task</returns>
    Task TrackCompletionTokenUsageAsync(Models.Completion.CompletionRequest request, Models.Completion.CompletionResponse response);

    /// <summary>
    /// Get token usage for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <returns>List of token usage records</returns>
    Task<IEnumerable<TokenUsageRecord>> GetUsageForUserAsync(string userId, DateTimeOffset startDate, DateTimeOffset endDate);

    /// <summary>
    /// Get token usage for an API key
    /// </summary>
    /// <param name="apiKeyId">API key ID</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <returns>List of token usage records</returns>
    Task<IEnumerable<TokenUsageRecord>> GetUsageForApiKeyAsync(string apiKeyId, DateTimeOffset startDate, DateTimeOffset endDate);

    /// <summary>
    /// Get token usage for a model
    /// </summary>
    /// <param name="modelId">Model ID</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <returns>List of token usage records</returns>
    Task<IEnumerable<TokenUsageRecord>> GetUsageForModelAsync(string modelId, DateTimeOffset startDate, DateTimeOffset endDate);

    /// <summary>
    /// Get token usage statistics
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="groupBy">Group by criteria (day, month, model, user)</param>
    /// <returns>Token usage statistics</returns>
    Task<IEnumerable<object>> GetTokenUsageStatisticsAsync(DateTimeOffset? startDate, DateTimeOffset? endDate, string? groupBy);

    /// <summary>
    /// Get token usage for a provider
    /// </summary>
    /// <param name="provider">Provider name</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <returns>List of token usage records</returns>
    Task<IEnumerable<TokenUsageRecord>> GetUsageForProviderAsync(string provider, DateTimeOffset startDate, DateTimeOffset endDate);

    /// <summary>
    /// Get total token usage
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <returns>List of token usage records</returns>
    Task<IEnumerable<TokenUsageRecord>> GetTotalUsageAsync(DateTimeOffset startDate, DateTimeOffset endDate);

    /// <summary>
    /// Get token usage summary
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <returns>Token usage summary</returns>
    Task<TokenUsageSummary> GetUsageSummaryAsync(DateTimeOffset startDate, DateTimeOffset endDate);
}
