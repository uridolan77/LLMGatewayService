using LLMGateway.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LLMGateway.Infrastructure.Persistence.Repositories;

/// <summary>
/// Interface for model repository
/// </summary>
public interface IModelRepository : IRepository<Model>
{
    /// <summary>
    /// Get models by provider
    /// </summary>
    /// <param name="provider">Provider</param>
    /// <param name="activeOnly">Whether to get only active models</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Models</returns>
    Task<IEnumerable<Model>> GetByProviderAsync(string provider, bool activeOnly = true, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get models by capability
    /// </summary>
    /// <param name="supportsCompletions">Whether the model supports completions</param>
    /// <param name="supportsEmbeddings">Whether the model supports embeddings</param>
    /// <param name="supportsStreaming">Whether the model supports streaming</param>
    /// <param name="supportsFunctionCalling">Whether the model supports function calling</param>
    /// <param name="supportsVision">Whether the model supports vision</param>
    /// <param name="activeOnly">Whether to get only active models</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Models</returns>
    Task<IEnumerable<Model>> GetByCapabilityAsync(
        bool? supportsCompletions = null,
        bool? supportsEmbeddings = null,
        bool? supportsStreaming = null,
        bool? supportsFunctionCalling = null,
        bool? supportsVision = null,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get model by provider and provider model ID
    /// </summary>
    /// <param name="provider">Provider</param>
    /// <param name="providerModelId">Provider model ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Model</returns>
    Task<Model?> GetByProviderModelAsync(string provider, string providerModelId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Model repository implementation
/// </summary>
public class ModelRepository : Repository<Model>, IModelRepository
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context">Database context</param>
    public ModelRepository(LLMGatewayDbContext context) : base(context)
    {
    }
    
    /// <inheritdoc/>
    public async Task<IEnumerable<Model>> GetByProviderAsync(string provider, bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(m => m.Provider == provider);
        
        if (activeOnly)
        {
            query = query.Where(m => m.IsActive);
        }
        
        return await query.ToListAsync(cancellationToken);
    }
    
    /// <inheritdoc/>
    public async Task<IEnumerable<Model>> GetByCapabilityAsync(
        bool? supportsCompletions = null,
        bool? supportsEmbeddings = null,
        bool? supportsStreaming = null,
        bool? supportsFunctionCalling = null,
        bool? supportsVision = null,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();
        
        if (supportsCompletions.HasValue)
        {
            query = query.Where(m => m.SupportsCompletions == supportsCompletions.Value);
        }
        
        if (supportsEmbeddings.HasValue)
        {
            query = query.Where(m => m.SupportsEmbeddings == supportsEmbeddings.Value);
        }
        
        if (supportsStreaming.HasValue)
        {
            query = query.Where(m => m.SupportsStreaming == supportsStreaming.Value);
        }
        
        if (supportsFunctionCalling.HasValue)
        {
            query = query.Where(m => m.SupportsFunctionCalling == supportsFunctionCalling.Value);
        }
        
        if (supportsVision.HasValue)
        {
            query = query.Where(m => m.SupportsVision == supportsVision.Value);
        }
        
        if (activeOnly)
        {
            query = query.Where(m => m.IsActive);
        }
        
        return await query.ToListAsync(cancellationToken);
    }
    
    /// <inheritdoc/>
    public async Task<Model?> GetByProviderModelAsync(string provider, string providerModelId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(m => m.Provider == provider && m.ProviderModelId == providerModelId, cancellationToken);
    }
}
