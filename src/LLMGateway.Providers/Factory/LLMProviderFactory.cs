using LLMGateway.Core.Exceptions;
using LLMGateway.Core.Interfaces;
using LLMGateway.Providers.Anthropic;
using LLMGateway.Providers.AzureOpenAI;
using LLMGateway.Providers.Cohere;
using LLMGateway.Providers.HuggingFace;
using LLMGateway.Providers.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LLMGateway.Providers.Factory;

/// <summary>
/// Factory for creating LLM providers
/// </summary>
public class LLMProviderFactory : ILLMProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LLMProviderFactory> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="serviceProvider">Service provider</param>
    /// <param name="logger">Logger</param>
    public LLMProviderFactory(
        IServiceProvider serviceProvider,
        ILogger<LLMProviderFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public ILLMProvider GetProvider(string providerName)
    {
        _logger.LogInformation("Getting provider {ProviderName}", providerName);

        ILLMProvider? provider = providerName.ToLowerInvariant() switch
        {
            "openai" => _serviceProvider.GetService<OpenAIProvider>(),
            "anthropic" => _serviceProvider.GetService<AnthropicProvider>(),
            "cohere" => _serviceProvider.GetService<CohereProvider>(),
            "huggingface" => _serviceProvider.GetService<HuggingFaceProvider>(),
            "azure-openai" => _serviceProvider.GetService<AzureOpenAIProvider>(),
            _ => null
        };

        if (provider == null)
        {
            _logger.LogError("Provider {ProviderName} not found", providerName);
            throw new ProviderNotFoundException(providerName);
        }

        return provider;
    }

    /// <inheritdoc/>
    public IEnumerable<ILLMProvider> GetAllProviders()
    {
        _logger.LogInformation("Getting all providers");

        var providers = new List<ILLMProvider>();

        // Get all registered providers
        var openAIProvider = _serviceProvider.GetService<OpenAIProvider>();
        if (openAIProvider != null)
        {
            providers.Add(openAIProvider);
        }

        var anthropicProvider = _serviceProvider.GetService<AnthropicProvider>();
        if (anthropicProvider != null)
        {
            providers.Add(anthropicProvider);
        }

        var cohereProvider = _serviceProvider.GetService<CohereProvider>();
        if (cohereProvider != null)
        {
            providers.Add(cohereProvider);
        }

        var huggingFaceProvider = _serviceProvider.GetService<HuggingFaceProvider>();
        if (huggingFaceProvider != null)
        {
            providers.Add(huggingFaceProvider);
        }

        var azureOpenAIProvider = _serviceProvider.GetService<AzureOpenAIProvider>();
        if (azureOpenAIProvider != null)
        {
            providers.Add(azureOpenAIProvider);
        }

        return providers;
    }
}
