using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Auth;
using Microsoft.Extensions.Logging;

namespace LLMGateway.Infrastructure.Auth;

/// <summary>
/// Authentication service implementation
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserService _userService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;
    
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="userService">User service</param>
    /// <param name="tokenService">Token service</param>
    /// <param name="logger">Logger</param>
    public AuthService(
        IUserService userService,
        ITokenService tokenService,
        ILogger<AuthService> logger)
    {
        _userService = userService;
        _tokenService = tokenService;
        _logger = logger;
    }
    
    /// <inheritdoc/>
    public async Task<LoginResponse?> LoginAsync(LoginRequest request, string ipAddress)
    {
        _logger.LogInformation("Login attempt for user: {Username}", request.Username);
        
        // Get user by username
        var user = await _userService.GetByUsernameAsync(request.Username);
        
        // Check if user exists
        if (user == null)
        {
            _logger.LogWarning("Login failed: User not found: {Username}", request.Username);
            return null;
        }
        
        // Check if user is active
        if (!user.IsActive)
        {
            _logger.LogWarning("Login failed: User is not active: {Username}", request.Username);
            return null;
        }
        
        // Verify password
        if (!await _userService.VerifyPasswordAsync(user, request.Password))
        {
            _logger.LogWarning("Login failed: Invalid password for user: {Username}", request.Username);
            return null;
        }
          // Generate JWT token
        var token = await _tokenService.GenerateAccessTokenAsync(user);
        
        // Generate refresh token
        var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user.Id, ipAddress);
        
        // Update last login time
        user.LastLogin = DateTime.UtcNow;
        await _userService.UpdateAsync(user);
        
        _logger.LogInformation("Login successful for user: {Username}", request.Username);
        
        // Return login response
        return new LoginResponse
        {
            UserId = user.Id,
            Username = user.Username,
            Roles = user.Roles,
            AccessToken = token,
            RefreshToken = refreshToken.Token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60) // TODO: Get from JWT options
        };
    }
    
    /// <inheritdoc/>
    public async Task<User> RegisterAsync(RegisterRequest request)
    {
        _logger.LogInformation("Registering new user: {Username}", request.Username);
          // Create user
        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            IsActive = true,
            Roles = new List<string> { "User" } // Default role
        };
        
        try
        {
            // Create user with password
            user = await _userService.CreateAsync(user, request.Password);
            
            _logger.LogInformation("User registered successfully: {Username}", request.Username);
            
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user: {Username}", request.Username);
            throw;
        }
    }
    
    /// <inheritdoc/>
    public async Task<LoginResponse?> RefreshTokenAsync(string token, string ipAddress)
    {
        _logger.LogInformation("Refreshing token");
        
        // Get refresh token
        var refreshToken = await _tokenService.GetRefreshTokenAsync(token);
        
        if (refreshToken == null)
        {
            _logger.LogWarning("Refresh token not found");
            return null;
        }
        
        // Check if token is active
        if (refreshToken.IsRevoked)
        {
            _logger.LogWarning("Refresh token is revoked");
            await RevokeDescendantRefreshTokensAsync(refreshToken, ipAddress, "Attempted reuse of revoked ancestor token");
            return null;
        }
        
        // Check if token is expired
        if (refreshToken.IsExpired)
        {
            _logger.LogWarning("Refresh token has expired");
            await _tokenService.RevokeTokenAsync(token, ipAddress, "Token expired");
            return null;
        }
        
        // Get user
        var user = await _userService.GetByIdAsync(refreshToken.UserId);
        
        if (user == null)
        {
            _logger.LogWarning("User not found for refresh token");
            return null;
        }
        
        // Check if user is active
        if (!user.IsActive)
        {
            _logger.LogWarning("User is not active for refresh token");
            return null;
        }
        
        // Generate new refresh token
        var newRefreshToken = await _tokenService.GenerateRefreshTokenAsync(user.Id, ipAddress);
        
        // Revoke the current refresh token
        await _tokenService.RevokeTokenAsync(token, ipAddress, "Replaced by new token");
        
        // Generate new JWT token
        var jwtToken = await _tokenService.GenerateAccessTokenAsync(user);
        
        _logger.LogInformation("Token refreshed successfully for user: {UserId}", user.Id);
          // Return login response
        return new LoginResponse
        {
            UserId = user.Id,
            Username = user.Username,
            Roles = user.Roles,
            AccessToken = jwtToken,
            RefreshToken = newRefreshToken.Token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60) // TODO: Get from JWT options
        };
    }
    
    /// <inheritdoc/>
    public async Task<bool> LogoutAsync(string userId, string ipAddress)
    {
        _logger.LogInformation("Logging out user: {UserId}", userId);
        
        // Revoke all refresh tokens for the user
        return await _tokenService.RevokeAllUserTokensAsync(userId, ipAddress, "Logout");
    }
    
    #region Private methods
    
    /// <summary>
    /// Revoke descendant refresh tokens
    /// </summary>
    /// <param name="refreshToken">Refresh token</param>
    /// <param name="ipAddress">IP address</param>
    /// <param name="reason">Reason</param>
    private async Task RevokeDescendantRefreshTokensAsync(
        RefreshToken refreshToken,
        string ipAddress,
        string reason)
    {
        // Revoke only the specified token
        await _tokenService.RevokeTokenAsync(refreshToken.Token, ipAddress, reason);
    }
    
    #endregion
}
