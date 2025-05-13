using LLMGateway.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace LLMGateway.Core.Services;

/// <summary>
/// Service for managing LLM providers
/// </summary>
public class ProviderService : IProviderService
{
    private readonly ILLMProviderFactory _providerFactory;
    private readonly ILogger<ProviderService> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="providerFactory">Provider factory</param>
    /// <param name="logger">Logger</param>
    public ProviderService(
        ILLMProviderFactory providerFactory,
        ILogger<ProviderService> logger)
    {
        _providerFactory = providerFactory;
        _logger = logger;
    }

    /// <inheritdoc/>
    public ILLMProvider GetProvider(string providerName)
    {
        try
        {
            return _providerFactory.GetProvider(providerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get provider {ProviderName}", providerName);
            throw;
        }
    }

    /// <inheritdoc/>
    public IEnumerable<ILLMProvider> GetAllProviders()
    {
        try
        {
            return _providerFactory.GetAllProviders();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all providers");
            throw;
        }
    }
}
