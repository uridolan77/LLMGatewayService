using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Auth;
using Microsoft.Extensions.Logging;

namespace LLMGateway.Core.Services;

/// <summary>
/// In-memory implementation of IAuthService for when database is not available
/// </summary>
public class InMemoryAuthService : IAuthService
{
    private readonly ILogger<InMemoryAuthService> _logger;
    private readonly IUserService _userService;
    private readonly ITokenService _tokenService;

    public InMemoryAuthService(
        ILogger<InMemoryAuthService> logger,
        IUserService userService,
        ITokenService tokenService)
    {
        _logger = logger;
        _userService = userService;
        _tokenService = tokenService;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, string ipAddress)
    {
        _logger.LogDebug("Attempting login for username {Username}", request.Username);

        try
        {
            // Get user by username or email
            var user = await _userService.GetByUsernameAsync(request.Username)
                      ?? await _userService.GetByEmailAsync(request.Username);

            if (user == null)
            {
                _logger.LogWarning("Login failed: User {Username} not found", request.Username);
                return null;
            }

            // Check if user is active
            if (!user.IsActive)
            {
                _logger.LogWarning("Login failed: User {Username} is not active", request.Username);
                return null;
            }

            // Validate password
            var isPasswordValid = await _userService.VerifyPasswordAsync(user, request.Password);
            if (!isPasswordValid)
            {
                _logger.LogWarning("Login failed: Invalid password for user {Username}", request.Username);
                return null;
            }

            // Generate tokens
            var accessToken = await _tokenService.GenerateAccessTokenAsync(user);
            var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user.Id, ipAddress);

            _logger.LogInformation("User {Username} logged in successfully", request.Username);

            return new LoginResponse
            {
                UserId = user.Id,
                Username = user.Username,
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60) // Default 60 minutes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for username {Username}", request.Username);
            return null;
        }
    }

    public async Task<LoginResponse?> RefreshTokenAsync(string refreshToken, string ipAddress)
    {
        _logger.LogDebug("Attempting to refresh token");

        try
        {
            // Get refresh token
            var token = await _tokenService.GetRefreshTokenAsync(refreshToken);
            if (token == null)
            {
                _logger.LogWarning("Refresh token not found");
                return null;
            }

            // Check if token is expired
            if (token.ExpiresAt <= DateTime.UtcNow)
            {
                _logger.LogWarning("Refresh token expired");
                return null;
            }

            // Check if token is revoked
            if (token.RevokedAt.HasValue)
            {
                _logger.LogWarning("Refresh token was revoked");
                return null;
            }

            // Get user
            var user = await _userService.GetByIdAsync(token.UserId);
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

            // Revoke old refresh token
            await _tokenService.RevokeTokenAsync(refreshToken, ipAddress, "Replaced by new token");

            // Generate new tokens
            var accessToken = await _tokenService.GenerateAccessTokenAsync(user);
            var newRefreshToken = await _tokenService.GenerateRefreshTokenAsync(user.Id, ipAddress);

            _logger.LogInformation("Token refreshed successfully for user {UserId}", user.Id);

            return new LoginResponse
            {
                UserId = user.Id,
                Username = user.Username,
                AccessToken = accessToken,
                RefreshToken = newRefreshToken.Token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60) // Default 60 minutes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return null;
        }
    }

    public async Task<bool> LogoutAsync(string userId, string ipAddress)
    {
        _logger.LogDebug("Attempting logout for user {UserId}", userId);

        try
        {
            // Revoke all user tokens
            await _tokenService.RevokeAllUserTokensAsync(userId, ipAddress, "User logout");

            _logger.LogInformation("User {UserId} logged out successfully", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout for user {UserId}", userId);
            return false;
        }
    }

    public async Task<User> RegisterAsync(RegisterRequest request)
    {
        _logger.LogDebug("Attempting registration for username {Username}", request.Username);

        try
        {
            // Check if user already exists
            var existingUser = await _userService.GetByUsernameAsync(request.Username);
            if (existingUser != null)
            {
                _logger.LogWarning("Registration failed: Username {Username} already exists", request.Username);
                throw new InvalidOperationException("Username already exists");
            }

            existingUser = await _userService.GetByEmailAsync(request.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Registration failed: Email {Email} already exists", request.Email);
                throw new InvalidOperationException("Email already exists");
            }

            // Create new user
            var newUser = new User
            {
                Username = request.Username,
                Email = request.Email,
                IsActive = true
            };

            var createdUser = await _userService.CreateAsync(newUser, request.Password);

            _logger.LogInformation("User {Username} registered successfully", request.Username);

            return createdUser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for username {Username}", request.Username);
            throw;
        }
    }


}
