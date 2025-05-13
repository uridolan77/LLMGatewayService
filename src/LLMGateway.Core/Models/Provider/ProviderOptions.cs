namespace LLMGateway.Core.Models.Provider;

/// <summary>
/// Base options for all providers
/// </summary>
public abstract class ProviderOptions
{
    /// <summary>
    /// API key for the provider
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// API URL for the provider
    /// </summary>
    public string ApiUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Timeout in seconds for requests
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// Model mappings for the provider
    /// </summary>
    public List<ProviderModelMapping> ModelMappings { get; set; } = new();
}

/// <summary>
/// Options for OpenAI provider
/// </summary>
public class OpenAIOptions : ProviderOptions
{
    /// <summary>
    /// Organization ID for OpenAI
    /// </summary>
    public string OrganizationId { get; set; } = string.Empty;
}

/// <summary>
/// Options for Anthropic provider
/// </summary>
public class AnthropicOptions : ProviderOptions
{
    /// <summary>
    /// API version for Anthropic
    /// </summary>
    public string ApiVersion { get; set; } = "2023-06-01";
}

/// <summary>
/// Options for Cohere provider
/// </summary>
public class CohereOptions : ProviderOptions
{
}

/// <summary>
/// Options for HuggingFace provider
/// </summary>
public class HuggingFaceOptions : ProviderOptions
{
}

/// <summary>
/// Model mapping for a provider
/// </summary>
public class ProviderModelMapping
{
    /// <summary>
    /// Gateway model ID
    /// </summary>
    public string GatewayModelId { get; set; } = string.Empty;
    
    /// <summary>
    /// Provider model ID
    /// </summary>
    public string ProviderModelId { get; set; } = string.Empty;
}
