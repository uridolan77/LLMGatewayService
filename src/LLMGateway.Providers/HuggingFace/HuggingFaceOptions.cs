namespace LLMGateway.Providers.HuggingFace;

/// <summary>
/// Options for the HuggingFace provider
/// </summary>
public class HuggingFaceOptions
{
    /// <summary>
    /// API key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// API URL
    /// </summary>
    public string ApiUrl { get; set; } = "https://api-inference.huggingface.co/models";
    
    /// <summary>
    /// Timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;
}
