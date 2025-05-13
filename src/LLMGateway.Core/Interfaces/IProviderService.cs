using LLMGateway.Core.Interfaces;

namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Service for managing LLM providers
/// </summary>
public interface IProviderService
{
    /// <summary>
    /// Get a provider by name
    /// </summary>
    /// <param name="providerName">Name of the provider</param>
    /// <returns>LLM provider</returns>
    ILLMProvider GetProvider(string providerName);
    
    /// <summary>
    /// Get all available providers
    /// </summary>
    /// <returns>List of LLM providers</returns>
    IEnumerable<ILLMProvider> GetAllProviders();
}
