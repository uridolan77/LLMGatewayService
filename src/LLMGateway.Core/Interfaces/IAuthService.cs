using LLMGateway.Core.Models.Auth;

namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Interface for authentication service
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Login user
    /// </summary>
    /// <param name="request">Login request</param>
    /// <param name="ipAddress">IP address</param>
    /// <returns>Login response</returns>
    Task<LoginResponse?> LoginAsync(LoginRequest request, string ipAddress);
    
    /// <summary>
    /// Register user
    /// </summary>
    /// <param name="request">Register request</param>
    /// <returns>User</returns>
    Task<User> RegisterAsync(RegisterRequest request);
    
    /// <summary>
    /// Refresh token
    /// </summary>
    /// <param name="token">Refresh token</param>
    /// <param name="ipAddress">IP address</param>
    /// <returns>Login response</returns>
    Task<LoginResponse?> RefreshTokenAsync(string token, string ipAddress);
    
    /// <summary>
    /// Logout user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="ipAddress">IP address</param>
    /// <returns>Whether the logout was successful</returns>
    Task<bool> LogoutAsync(string userId, string ipAddress);
}
