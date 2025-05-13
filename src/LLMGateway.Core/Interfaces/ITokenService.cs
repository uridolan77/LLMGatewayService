using LLMGateway.Core.Models.Auth;
using System.Security.Claims;

namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Interface for token service
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generate access token for a user
    /// </summary>
    /// <param name="user">User</param>
    /// <returns>Access token</returns>
    Task<string> GenerateAccessTokenAsync(User user);
    
    /// <summary>
    /// Generate refresh token for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="ipAddress">IP address</param>
    /// <returns>Refresh token</returns>
    Task<RefreshToken> GenerateRefreshTokenAsync(string userId, string ipAddress);
    
    /// <summary>
    /// Validate token
    /// </summary>
    /// <param name="token">Token</param>
    /// <returns>Whether the token is valid</returns>
    Task<bool> ValidateTokenAsync(string token);
    
    /// <summary>
    /// Get principal from token
    /// </summary>
    /// <param name="token">Token</param>
    /// <returns>Claims principal</returns>
    Task<ClaimsPrincipal> GetPrincipalFromTokenAsync(string token);
    
    /// <summary>
    /// Get refresh token by token
    /// </summary>
    /// <param name="token">Token</param>
    /// <returns>Refresh token</returns>
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    
    /// <summary>
    /// Revoke refresh token
    /// </summary>
    /// <param name="token">Token</param>
    /// <param name="ipAddress">IP address</param>
    /// <param name="reason">Reason for revocation</param>
    /// <returns>Whether the token was revoked</returns>
    Task<bool> RevokeTokenAsync(string token, string ipAddress, string? reason = null);
    
    /// <summary>
    /// Revoke all refresh tokens for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="ipAddress">IP address</param>
    /// <param name="reason">Reason for revocation</param>
    /// <returns>Whether the tokens were revoked</returns>
    Task<bool> RevokeAllUserTokensAsync(string userId, string ipAddress, string? reason = null);
}
