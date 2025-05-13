namespace LLMGateway.Providers.Anthropic;

/// <summary>
/// Options for the Anthropic provider
/// </summary>
public class AnthropicOptions
{
    /// <summary>
    /// API key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// API URL
    /// </summary>
    public string ApiUrl { get; set; } = "https://api.anthropic.com";
    
    /// <summary>
    /// Timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// API version
    /// </summary>
    public string ApiVersion { get; set; } = "2023-06-01";
    
    /// <summary>
    /// Messages endpoint
    /// </summary>
    public string MessagesEndpoint { get; set; } = "/v1/messages";
}
