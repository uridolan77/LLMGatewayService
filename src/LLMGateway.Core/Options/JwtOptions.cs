namespace LLMGateway.Core.Options;

/// <summary>
/// Options for JWT authentication
/// </summary>
public class JwtOptions
{
    /// <summary>
    /// JWT secret key
    /// </summary>
    public string Secret { get; set; } = string.Empty;
    
    /// <summary>
    /// JWT issuer
    /// </summary>
    public string Issuer { get; set; } = string.Empty;
    
    /// <summary>
    /// JWT audience
    /// </summary>
    public string Audience { get; set; } = string.Empty;
    
    /// <summary>
    /// JWT expiry time in minutes
    /// </summary>
    public int ExpiryMinutes { get; set; } = 60;
}
