using LLMGateway.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LLMGateway.Infrastructure.Persistence.Repositories;

/// <summary>
/// Interface for setting repository
/// </summary>
public interface ISettingRepository : IRepository<Setting>
{
    /// <summary>
    /// Get setting by key
    /// </summary>
    /// <param name="key">Setting key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Setting</returns>
    Task<Setting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get settings by category
    /// </summary>
    /// <param name="category">Category</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Settings</returns>
    Task<IEnumerable<Setting>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get setting value by key
    /// </summary>
    /// <param name="key">Setting key</param>
    /// <param name="defaultValue">Default value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Setting value</returns>
    Task<string?> GetValueAsync(string key, string? defaultValue = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Set setting value
    /// </summary>
    /// <param name="key">Setting key</param>
    /// <param name="value">Setting value</param>
    /// <param name="category">Category</param>
    /// <param name="description">Description</param>
    /// <param name="modifiedBy">Modified by</param>
    /// <param name="isEncrypted">Whether the value is encrypted</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Setting</returns>
    Task<Setting> SetValueAsync(string key, string? value, string category, string? description = null, string? modifiedBy = null, bool isEncrypted = false, CancellationToken cancellationToken = default);
}

/// <summary>
/// Setting repository implementation
/// </summary>
public class SettingRepository : Repository<Setting>, ISettingRepository
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context">Database context</param>
    public SettingRepository(LLMGatewayDbContext context) : base(context)
    {
    }
    
    /// <inheritdoc/>
    public async Task<Setting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
    }
    
    /// <inheritdoc/>
    public async Task<IEnumerable<Setting>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(s => s.Category == category).ToListAsync(cancellationToken);
    }
    
    /// <inheritdoc/>
    public async Task<string?> GetValueAsync(string key, string? defaultValue = null, CancellationToken cancellationToken = default)
    {
        var setting = await GetByKeyAsync(key, cancellationToken);
        return setting?.Value ?? defaultValue;
    }
    
    /// <inheritdoc/>
    public async Task<Setting> SetValueAsync(string key, string? value, string category, string? description = null, string? modifiedBy = null, bool isEncrypted = false, CancellationToken cancellationToken = default)
    {
        var setting = await GetByKeyAsync(key, cancellationToken);
        
        if (setting == null)
        {
            setting = new Setting
            {
                Key = key,
                Value = value,
                Category = category,
                Description = description,
                IsEncrypted = isEncrypted,
                ModifiedBy = modifiedBy,
                LastModified = DateTimeOffset.UtcNow
            };
            
            await _dbSet.AddAsync(setting, cancellationToken);
        }
        else
        {
            setting.Value = value;
            setting.Category = category;
            setting.IsEncrypted = isEncrypted;
            setting.LastModified = DateTimeOffset.UtcNow;
            setting.ModifiedBy = modifiedBy;
            
            if (!string.IsNullOrEmpty(description))
            {
                setting.Description = description;
            }
            
            _dbSet.Update(setting);
        }
        
        await _context.SaveChangesAsync(cancellationToken);
        return setting;
    }
}
