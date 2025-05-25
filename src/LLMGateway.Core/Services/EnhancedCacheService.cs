using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using LLMGateway.Core.Interfaces;

namespace LLMGateway.Core.Services;

/// <summary>
/// Enhanced caching service with sliding expiration and advanced features
/// </summary>
public class EnhancedCacheService : IEnhancedCacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<EnhancedCacheService> _logger;
    private long _hits = 0;
    private long _misses = 0;
    private long _evictions = 0;
    private long _expirations = 0;
    private readonly ConcurrentDictionary<string, DateTime> _keyTimestamps = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public EnhancedCacheService(
        IDistributedCache distributedCache,
        ILogger<EnhancedCacheService> logger)
    {
        _distributedCache = distributedCache;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var cachedBytes = await _distributedCache.GetAsync(key);
            if (cachedBytes != null)
            {
                Interlocked.Increment(ref _hits);
                var cachedValue = JsonSerializer.Deserialize<T>(cachedBytes);
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return cachedValue;
            }

            Interlocked.Increment(ref _misses);
            _logger.LogDebug("Cache miss for key: {Key}", key);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached value for key: {Key}", key);
            Interlocked.Increment(ref _misses);
            return default;
        }
    }

    /// <inheritdoc/>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        try
        {
            var serializedValue = JsonSerializer.SerializeToUtf8Bytes(value);
            var options = new DistributedCacheEntryOptions();

            if (expiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiration.Value;
            }

            await _distributedCache.SetAsync(key, serializedValue, options);
            _keyTimestamps.TryAdd(key, DateTime.UtcNow);

            _logger.LogDebug("Cached value for key: {Key} with expiration: {Expiration}", key, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cached value for key: {Key}", key);
        }
    }

    /// <inheritdoc/>
    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? slidingExpiration = null, TimeSpan? absoluteExpiration = null)
    {
        var cached = await GetAsync<T>(key);
        if (cached != null)
        {
            return cached;
        }

        await _semaphore.WaitAsync();
        try
        {
            // Double-check pattern to avoid race conditions
            cached = await GetAsync<T>(key);
            if (cached != null)
            {
                return cached;
            }

            _logger.LogDebug("Cache miss for key: {Key}, executing factory function", key);
            var value = await factory();

            if (value != null)
            {
                await SetWithSlidingExpirationAsync(key, value, slidingExpiration ?? TimeSpan.FromMinutes(5), absoluteExpiration);
            }

            return value;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task SetWithSlidingExpirationAsync<T>(string key, T value, TimeSpan slidingExpiration, TimeSpan? absoluteExpiration = null)
    {
        try
        {
            var serializedValue = JsonSerializer.SerializeToUtf8Bytes(value);
            var options = new DistributedCacheEntryOptions
            {
                SlidingExpiration = slidingExpiration
            };

            if (absoluteExpiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = absoluteExpiration.Value;
            }

            await _distributedCache.SetAsync(key, serializedValue, options);
            _keyTimestamps.TryAdd(key, DateTime.UtcNow);

            _logger.LogDebug("Cached value for key: {Key} with sliding expiration: {SlidingExpiration}", key, slidingExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cached value with sliding expiration for key: {Key}", key);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> RefreshAsync(string key, TimeSpan? slidingExpiration = null)
    {
        try
        {
            var cachedBytes = await _distributedCache.GetAsync(key);
            if (cachedBytes == null)
            {
                return false;
            }

            var options = new DistributedCacheEntryOptions();
            if (slidingExpiration.HasValue)
            {
                options.SlidingExpiration = slidingExpiration.Value;
            }
            else
            {
                options.SlidingExpiration = TimeSpan.FromMinutes(5); // Default
            }

            await _distributedCache.SetAsync(key, cachedBytes, options);
            _keyTimestamps.TryUpdate(key, DateTime.UtcNow, _keyTimestamps.GetValueOrDefault(key));

            _logger.LogDebug("Refreshed cache entry for key: {Key}", key);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing cached value for key: {Key}", key);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task RemoveAsync(string key)
    {
        try
        {
            await _distributedCache.RemoveAsync(key);
            _keyTimestamps.TryRemove(key, out _);
            _logger.LogDebug("Removed cached value for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cached value for key: {Key}", key);
        }
    }

    /// <inheritdoc/>
    public async Task<CacheStatistics> GetStatisticsAsync()
    {
        await Task.CompletedTask; // Make it async for future enhancements

        return new CacheStatistics
        {
            Hits = _hits,
            Misses = _misses,
            ItemCount = _keyTimestamps.Count,
            EstimatedMemoryUsage = EstimateMemoryUsage(),
            Evictions = _evictions,
            Expirations = _expirations
        };
    }

    /// <inheritdoc/>
    public async Task<int> RemoveByPatternAsync(string pattern)
    {
        try
        {
            var keysToRemove = _keyTimestamps.Keys
                .Where(key => IsPatternMatch(key, pattern))
                .ToList();

            var removeTasks = keysToRemove.Select(RemoveAsync);
            await Task.WhenAll(removeTasks);

            _logger.LogDebug("Removed {Count} cached entries matching pattern: {Pattern}", keysToRemove.Count, pattern);
            return keysToRemove.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cached values by pattern: {Pattern}", pattern);
            return 0;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            var cachedBytes = await _distributedCache.GetAsync(key);
            return cachedBytes != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if key exists: {Key}", key);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<TimeSpan?> GetTimeToLiveAsync(string key)
    {
        // This is a limitation of IDistributedCache - it doesn't provide TTL information
        // In a real implementation, you might use Redis directly or store TTL metadata
        await Task.CompletedTask;
        return null;
    }

    private long EstimateMemoryUsage()
    {
        // Rough estimation - in a real implementation, you'd want more accurate measurement
        return _keyTimestamps.Count * 1024; // Assume 1KB per cached item on average
    }

    private bool IsPatternMatch(string key, string pattern)
    {
        // Simple wildcard matching - could be enhanced with regex
        if (pattern.Contains('*'))
        {
            var parts = pattern.Split('*');
            if (parts.Length == 2)
            {
                return key.StartsWith(parts[0]) && key.EndsWith(parts[1]);
            }
        }

        return key.Equals(pattern, StringComparison.OrdinalIgnoreCase);
    }
}
