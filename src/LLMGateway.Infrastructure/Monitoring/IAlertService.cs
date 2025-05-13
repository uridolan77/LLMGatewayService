namespace LLMGateway.Infrastructure.Monitoring;

/// <summary>
/// Interface for alert service
/// </summary>
public interface IAlertService
{
    /// <summary>
    /// Send a provider unavailable alert
    /// </summary>
    /// <param name="provider">Provider</param>
    /// <param name="errorMessage">Error message</param>
    /// <returns>Task</returns>
    Task SendProviderUnavailableAlertAsync(string provider, string? errorMessage = null);
    
    /// <summary>
    /// Send a model performance alert
    /// </summary>
    /// <param name="modelId">Model ID</param>
    /// <param name="provider">Provider</param>
    /// <param name="successRate">Success rate</param>
    /// <param name="averageResponseTimeMs">Average response time in milliseconds</param>
    /// <returns>Task</returns>
    Task SendModelPerformanceAlertAsync(string modelId, string provider, double successRate, double averageResponseTimeMs);
    
    /// <summary>
    /// Send a token usage alert
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="apiKeyId">API key ID</param>
    /// <param name="usagePercentage">Usage percentage</param>
    /// <param name="limit">Limit</param>
    /// <returns>Task</returns>
    Task SendTokenUsageAlertAsync(string userId, string apiKeyId, double usagePercentage, int limit);
}
