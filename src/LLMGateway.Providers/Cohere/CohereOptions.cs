namespace LLMGateway.Providers.Cohere;

/// <summary>
/// Options for the Cohere provider
/// </summary>
public class CohereOptions
{
    /// <summary>
    /// API key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// API URL
    /// </summary>
    public string ApiUrl { get; set; } = "https://api.cohere.ai";
    
    /// <summary>
    /// Timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
