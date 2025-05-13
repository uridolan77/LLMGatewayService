namespace LLMGateway.Infrastructure.Monitoring;

/// <summary>
/// Interface for provider health monitor
/// </summary>
public interface IProviderHealthMonitor
{
    /// <summary>
    /// Start monitoring
    /// </summary>
    void Start();
    
    /// <summary>
    /// Stop monitoring
    /// </summary>
    void Stop();
    
    /// <summary>
    /// Check provider health
    /// </summary>
    /// <returns>Provider health status</returns>
    Task<Dictionary<string, bool>> CheckProvidersAsync();
    
    /// <summary>
    /// Get provider health status
    /// </summary>
    /// <returns>Provider health status</returns>
    Dictionary<string, bool> GetProviderHealthStatus();
}
