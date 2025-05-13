namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Service for caching data
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Get a value from the cache
    /// </summary>
    /// <typeparam name="T">Type of the value</typeparam>
    /// <param name="key">Cache key</param>
    /// <returns>Cached value or default</returns>
    Task<T?> GetAsync<T>(string key);
    
    /// <summary>
    /// Set a value in the cache
    /// </summary>
    /// <typeparam name="T">Type of the value</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="value">Value to cache</param>
    /// <param name="expirationTime">Expiration time</param>
    /// <returns>Task</returns>
    Task SetAsync<T>(string key, T value, TimeSpan? expirationTime = null);
    
    /// <summary>
    /// Remove a value from the cache
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <returns>Task</returns>
    Task RemoveAsync(string key);
    
    /// <summary>
    /// Check if a key exists in the cache
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <returns>True if the key exists</returns>
    Task<bool> ExistsAsync(string key);
}
