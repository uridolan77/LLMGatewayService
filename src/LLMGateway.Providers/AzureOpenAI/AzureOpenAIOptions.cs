namespace LLMGateway.Providers.AzureOpenAI;

/// <summary>
/// Azure OpenAI options
/// </summary>
public class AzureOpenAIOptions
{
    /// <summary>
    /// Azure OpenAI endpoint
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;
    
    /// <summary>
    /// Azure OpenAI API key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Azure OpenAI API version
    /// </summary>
    public string ApiVersion { get; set; } = "2023-05-15";
    
    /// <summary>
    /// Timeout in seconds for requests
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// Timeout in seconds for streaming requests
    /// </summary>
    public int StreamTimeoutSeconds { get; set; } = 120;
    
    /// <summary>
    /// Azure OpenAI deployments
    /// </summary>
    public List<AzureOpenAIDeployment> Deployments { get; set; } = new();
}

/// <summary>
/// Azure OpenAI deployment
/// </summary>
public class AzureOpenAIDeployment
{
    /// <summary>
    /// Deployment ID
    /// </summary>
    public string DeploymentId { get; set; } = string.Empty;
    
    /// <summary>
    /// Display name
    /// </summary>
    public string? DisplayName { get; set; }
    
    /// <summary>
    /// Model name
    /// </summary>
    public string ModelName { get; set; } = string.Empty;
    
    /// <summary>
    /// Deployment type
    /// </summary>
    public AzureOpenAIDeploymentType Type { get; set; } = AzureOpenAIDeploymentType.Completion;
}

/// <summary>
/// Azure OpenAI deployment type
/// </summary>
public enum AzureOpenAIDeploymentType
{
    /// <summary>
    /// Completion deployment
    /// </summary>
    Completion,
    
    /// <summary>
    /// Embedding deployment
    /// </summary>
    Embedding
}
