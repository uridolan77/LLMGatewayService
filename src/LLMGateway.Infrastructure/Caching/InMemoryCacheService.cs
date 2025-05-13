using LLMGateway.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LLMGateway.Infrastructure.Caching;

/// <summary>
/// In-memory cache service
/// </summary>
public class InMemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<InMemoryCacheService> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="cache">Memory cache</param>
    /// <param name="logger">Logger</param>
    public InMemoryCacheService(
        IMemoryCache cache,
        ILogger<InMemoryCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<T?> GetAsync<T>(string key)
    {
        _logger.LogDebug("Getting value from cache with key {Key}", key);
        
        if (_cache.TryGetValue(key, out T? value))
        {
            _logger.LogDebug("Cache hit for key {Key}", key);
            return Task.FromResult(value);
        }
        
        _logger.LogDebug("Cache miss for key {Key}", key);
        return Task.FromResult<T?>(default);
    }

    /// <inheritdoc/>
    public Task SetAsync<T>(string key, T value, TimeSpan? expirationTime = null)
    {
        _logger.LogDebug("Setting value in cache with key {Key}", key);
        
        var options = new MemoryCacheEntryOptions();
        
        if (expirationTime.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = expirationTime;
        }
        else
        {
            options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60);
        }
        
        _cache.Set(key, value, options);
        
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task RemoveAsync(string key)
    {
        _logger.LogDebug("Removing value from cache with key {Key}", key);
        
        _cache.Remove(key);
        
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> ExistsAsync(string key)
    {
        _logger.LogDebug("Checking if key {Key} exists in cache", key);
        
        return Task.FromResult(_cache.TryGetValue(key, out _));
    }
}
