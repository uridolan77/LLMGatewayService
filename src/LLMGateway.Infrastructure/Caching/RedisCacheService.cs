using LLMGateway.Core.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace LLMGateway.Infrastructure.Caching;

/// <summary>
/// Redis cache service
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly string _instanceName;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="redis">Redis connection multiplexer</param>
    /// <param name="logger">Logger</param>
    /// <param name="instanceName">Instance name</param>
    public RedisCacheService(
        IConnectionMultiplexer redis,
        ILogger<RedisCacheService> logger,
        string instanceName = "LLMGateway:")
    {
        _redis = redis;
        _logger = logger;
        _instanceName = instanceName;
    }

    /// <inheritdoc/>
    public async Task<T?> GetAsync<T>(string key)
    {
        _logger.LogDebug("Getting value from Redis cache with key {Key}", key);
        
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(GetFullKey(key));
        
        if (value.IsNullOrEmpty)
        {
            _logger.LogDebug("Redis cache miss for key {Key}", key);
            return default;
        }
        
        _logger.LogDebug("Redis cache hit for key {Key}", key);
        return JsonSerializer.Deserialize<T>(value!);
    }

    /// <inheritdoc/>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expirationTime = null)
    {
        _logger.LogDebug("Setting value in Redis cache with key {Key}", key);
        
        var db = _redis.GetDatabase();
        var serializedValue = JsonSerializer.Serialize(value);
        
        await db.StringSetAsync(
            GetFullKey(key),
            serializedValue,
            expirationTime ?? TimeSpan.FromMinutes(60));
    }

    /// <inheritdoc/>
    public async Task RemoveAsync(string key)
    {
        _logger.LogDebug("Removing value from Redis cache with key {Key}", key);
        
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(GetFullKey(key));
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(string key)
    {
        _logger.LogDebug("Checking if key {Key} exists in Redis cache", key);
        
        var db = _redis.GetDatabase();
        return await db.KeyExistsAsync(GetFullKey(key));
    }

    private string GetFullKey(string key)
    {
        return $"{_instanceName}{key}";
    }
}
