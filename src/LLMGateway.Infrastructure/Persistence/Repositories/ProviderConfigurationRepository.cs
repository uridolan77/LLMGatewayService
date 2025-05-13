using LLMGateway.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LLMGateway.Infrastructure.Persistence.Repositories;

/// <summary>
/// Interface for provider configuration repository
/// </summary>
public interface IProviderConfigurationRepository : IRepository<ProviderConfiguration>
{
    /// <summary>
    /// Get provider configuration by provider
    /// </summary>
    /// <param name="provider">Provider</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Provider configuration</returns>
    Task<ProviderConfiguration?> GetByProviderAsync(string provider, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get active provider configurations
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Active provider configurations</returns>
    Task<IEnumerable<ProviderConfiguration>> GetActiveAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Provider configuration repository implementation
/// </summary>
public class ProviderConfigurationRepository : Repository<ProviderConfiguration>, IProviderConfigurationRepository
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context">Database context</param>
    public ProviderConfigurationRepository(LLMGatewayDbContext context) : base(context)
    {
    }
    
    /// <inheritdoc/>
    public async Task<ProviderConfiguration?> GetByProviderAsync(string provider, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(p => p.Provider == provider, cancellationToken);
    }
    
    /// <inheritdoc/>
    public async Task<IEnumerable<ProviderConfiguration>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(p => p.IsActive).ToListAsync(cancellationToken);
    }
}
