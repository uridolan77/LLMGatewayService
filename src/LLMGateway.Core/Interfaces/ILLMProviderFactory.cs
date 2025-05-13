namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Factory for creating LLM providers
/// </summary>
public interface ILLMProviderFactory
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
