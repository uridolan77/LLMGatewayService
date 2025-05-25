using System;
using System.Threading.Tasks;

namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Enhanced caching service with advanced features like sliding expiration and cache-aside pattern
/// </summary>
public interface IEnhancedCacheService : ICacheService
{
    /// <summary>
    /// Get or set a cached value using the cache-aside pattern
    /// </summary>
    /// <typeparam name="T">Type of the cached value</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="factory">Factory function to create the value if not cached</param>
    /// <param name="slidingExpiration">Sliding expiration time</param>
    /// <param name="absoluteExpiration">Absolute expiration time</param>
    /// <returns>The cached or newly created value</returns>
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? slidingExpiration = null, TimeSpan? absoluteExpiration = null);

    /// <summary>
    /// Set a value with sliding expiration
    /// </summary>
    /// <typeparam name="T">Type of the value</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="value">Value to cache</param>
    /// <param name="slidingExpiration">Sliding expiration time</param>
    /// <param name="absoluteExpiration">Absolute expiration time</param>
    /// <returns>Task representing the operation</returns>
    Task SetWithSlidingExpirationAsync<T>(string key, T value, TimeSpan slidingExpiration, TimeSpan? absoluteExpiration = null);

    /// <summary>
    /// Refresh the expiration time of a cached item
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="slidingExpiration">New sliding expiration time</param>
    /// <returns>True if the item was refreshed, false if not found</returns>
    Task<bool> RefreshAsync(string key, TimeSpan? slidingExpiration = null);

    /// <summary>
    /// Get cache statistics
    /// </summary>
    /// <returns>Cache statistics</returns>
    Task<CacheStatistics> GetStatisticsAsync();

    /// <summary>
    /// Clear all cached items matching a pattern
    /// </summary>
    /// <param name="pattern">Pattern to match (supports wildcards)</param>
    /// <returns>Number of items removed</returns>
    Task<int> RemoveByPatternAsync(string pattern);

    /// <summary>
    /// Check if a key exists in the cache
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <returns>True if the key exists</returns>
    new Task<bool> ExistsAsync(string key);

    /// <summary>
    /// Get the remaining time to live for a cached item
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <returns>Time to live, or null if the item doesn't exist or has no expiration</returns>
    Task<TimeSpan?> GetTimeToLiveAsync(string key);
}

/// <summary>
/// Cache statistics
/// </summary>
public class CacheStatistics
{
    /// <summary>
    /// Total number of cache hits
    /// </summary>
    public long Hits { get; set; }

    /// <summary>
    /// Total number of cache misses
    /// </summary>
    public long Misses { get; set; }

    /// <summary>
    /// Cache hit ratio (0.0 to 1.0)
    /// </summary>
    public double HitRatio => (Hits + Misses) > 0 ? (double)Hits / (Hits + Misses) : 0.0;

    /// <summary>
    /// Total number of items in cache
    /// </summary>
    public long ItemCount { get; set; }

    /// <summary>
    /// Estimated memory usage in bytes
    /// </summary>
    public long EstimatedMemoryUsage { get; set; }

    /// <summary>
    /// Number of evictions due to memory pressure
    /// </summary>
    public long Evictions { get; set; }

    /// <summary>
    /// Number of expired items removed
    /// </summary>
    public long Expirations { get; set; }
}
