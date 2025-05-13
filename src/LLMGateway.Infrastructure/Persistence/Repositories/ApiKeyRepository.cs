using LLMGateway.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace LLMGateway.Infrastructure.Persistence.Repositories;

/// <summary>
/// Interface for API key repository
/// </summary>
public interface IApiKeyRepository : IRepository<ApiKey>
{
    /// <summary>
    /// Get API key by key
    /// </summary>
    /// <param name="key">API key</param>
    /// <returns>API key</returns>
    Task<ApiKey?> GetByKeyAsync(string key);
    
    /// <summary>
    /// Get API keys by user ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>API keys</returns>
    Task<IEnumerable<ApiKey>> GetByUserIdAsync(string userId);
    
    /// <summary>
    /// Get active API keys
    /// </summary>
    /// <returns>Active API keys</returns>
    Task<IEnumerable<ApiKey>> GetActiveKeysAsync();
    
    /// <summary>
    /// Get expired API keys
    /// </summary>
    /// <returns>Expired API keys</returns>
    Task<IEnumerable<ApiKey>> GetExpiredKeysAsync();
    
    /// <summary>
    /// Deactivate API key
    /// </summary>
    /// <param name="keyId">API key ID</param>
    /// <returns>Task</returns>
    Task DeactivateKeyAsync(string keyId);
}

/// <summary>
/// API key repository
/// </summary>
public class ApiKeyRepository : Repository<ApiKey>, IApiKeyRepository
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context">Database context</param>
    public ApiKeyRepository(LLMGatewayDbContext context) : base(context)
    {
    }
    
    /// <inheritdoc/>
    public async Task<ApiKey?> GetByKeyAsync(string key)
    {
        return await _dbSet.FirstOrDefaultAsync(k => k.Key == key);
    }
    
    /// <inheritdoc/>
    public async Task<IEnumerable<ApiKey>> GetByUserIdAsync(string userId)
    {
        return await _dbSet.Where(k => k.UserId == userId).ToListAsync();
    }
    
    /// <inheritdoc/>
    public async Task<IEnumerable<ApiKey>> GetActiveKeysAsync()
    {
        return await _dbSet.Where(k => k.IsActive && (k.ExpiresAt == null || k.ExpiresAt > DateTimeOffset.UtcNow)).ToListAsync();
    }
    
    /// <inheritdoc/>
    public async Task<IEnumerable<ApiKey>> GetExpiredKeysAsync()
    {
        return await _dbSet.Where(k => k.ExpiresAt != null && k.ExpiresAt <= DateTimeOffset.UtcNow).ToListAsync();
    }
    
    /// <inheritdoc/>
    public async Task DeactivateKeyAsync(string keyId)
    {
        var key = await GetByIdAsync(keyId);
        if (key != null)
        {
            key.IsActive = false;
            await UpdateAsync(key);
        }
    }
}
