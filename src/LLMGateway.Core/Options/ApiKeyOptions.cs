namespace LLMGateway.Core.Options;

/// <summary>
/// Options for API key authentication
/// </summary>
public class ApiKeyOptions
{
    /// <summary>
    /// List of API keys
    /// </summary>
    public List<ApiKey> ApiKeys { get; set; } = new();
}

/// <summary>
/// API key configuration
/// </summary>
public class ApiKey
{
    /// <summary>
    /// API key ID
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// API key value
    /// </summary>
    public string Key { get; set; } = string.Empty;
    
    /// <summary>
    /// API key owner
    /// </summary>
    public string Owner { get; set; } = string.Empty;
    
    /// <summary>
    /// API key permissions
    /// </summary>
    public List<string> Permissions { get; set; } = new();
    
    /// <summary>
    /// Token limits for the API key
    /// </summary>
    public TokenLimits TokenLimits { get; set; } = new();
}

/// <summary>
/// Token limits for an API key
/// </summary>
public class TokenLimits
{
    /// <summary>
    /// Daily token limit
    /// </summary>
    public int DailyLimit { get; set; } = 100000;
    
    /// <summary>
    /// Monthly token limit
    /// </summary>
    public int MonthlyLimit { get; set; } = 2000000;
}
