namespace LLMGateway.Core.Models.Auth;

/// <summary>
/// Result of authentication operations
/// </summary>
public class AuthResult
{
    /// <summary>
    /// Whether the authentication was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if authentication failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Access token if authentication was successful
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Refresh token if authentication was successful
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// User information if authentication was successful
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Token expiration time
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Create a successful authentication result
    /// </summary>
    /// <param name="accessToken">Access token</param>
    /// <param name="refreshToken">Refresh token</param>
    /// <param name="user">User information</param>
    /// <param name="expiresAt">Token expiration time</param>
    /// <returns>Successful authentication result</returns>
    public static AuthResult Success(string accessToken, string refreshToken, User user, DateTime? expiresAt = null)
    {
        return new AuthResult
        {
            IsSuccess = true,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = user,
            ExpiresAt = expiresAt
        };
    }

    /// <summary>
    /// Create a failed authentication result
    /// </summary>
    /// <param name="errorMessage">Error message</param>
    /// <returns>Failed authentication result</returns>
    public static AuthResult Failed(string errorMessage)
    {
        return new AuthResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}
