using LLMGateway.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace LLMGateway.Infrastructure.Caching;

/// <summary>
/// Extensions for caching
/// </summary>
public static class CacheExtensions
{
    /// <summary>
    /// Add Redis cache
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddRedisCache(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetValue<string>("Redis:ConnectionString");
        var instanceName = configuration.GetValue<string>("Redis:InstanceName") ?? "LLMGateway:";

        if (string.IsNullOrEmpty(connectionString))
        {
            // Use in-memory cache if Redis is not configured
            services.AddMemoryCache();

            // Register IDistributedCache for EnhancedCacheService
            services.AddDistributedMemoryCache();

            // Register custom ICacheService
            services.AddSingleton<ICacheService, InMemoryCacheService>();
            return services;
        }

        // Configure Redis for IDistributedCache (used by EnhancedCacheService)
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = connectionString;
            options.InstanceName = instanceName;
        });

        // Configure Redis for custom ICacheService
        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(connectionString));

        services.AddSingleton<ICacheService>(sp =>
        {
            var redis = sp.GetRequiredService<IConnectionMultiplexer>();
            var logger = sp.GetRequiredService<ILogger<RedisCacheService>>();
            return new RedisCacheService(redis, logger, instanceName);
        });

        return services;
    }
}
