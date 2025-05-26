using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Auth;
using LLMGateway.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace LLMGateway.Core.Services;

/// <summary>
/// In-memory implementation of ITokenService for when database is not available
/// </summary>
public class InMemoryTokenService : ITokenService
{
    private readonly ILogger<InMemoryTokenService> _logger;
    private readonly JwtOptions _jwtOptions;
    private readonly ConcurrentDictionary<string, RefreshToken> _refreshTokens = new();
    private readonly ConcurrentDictionary<string, DateTime> _blacklistedTokens = new();

    public InMemoryTokenService(
        ILogger<InMemoryTokenService> logger,
        IOptions<JwtOptions> jwtOptions)
    {
        _logger = logger;
        _jwtOptions = jwtOptions.Value;
    }

    public Task<string> GenerateAccessTokenAsync(User user)
    {
        _logger.LogDebug("Generating access token for user {UserId}", user.Id);

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtOptions.Secret);

        // Create claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, "User"), // Default role for in-memory implementation
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Create token descriptor
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature),
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience
        };

        // Create token
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return Task.FromResult(tokenHandler.WriteToken(token));
    }

    public Task<RefreshToken> GenerateRefreshTokenAsync(string userId, string ipAddress)
    {
        _logger.LogDebug("Generating refresh token for user {UserId}", userId);

        // Generate random token
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var refreshToken = Convert.ToBase64String(randomBytes);

        // Create refresh token
        var token = new RefreshToken
        {
            UserId = userId,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7), // 7 days expiry for refresh tokens
            CreatedByIp = ipAddress,
            CreatedAt = DateTime.UtcNow
        };

        _refreshTokens[refreshToken] = token;

        return Task.FromResult(token);
    }

    public Task<bool> ValidateTokenAsync(string token)
    {
        _logger.LogDebug("Validating token");

        try
        {
            // Check if token is blacklisted
            if (_blacklistedTokens.ContainsKey(token))
            {
                return Task.FromResult(false);
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtOptions.Secret);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtOptions.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return Task.FromResult(false);
        }
    }

    public Task<ClaimsPrincipal> GetPrincipalFromTokenAsync(string token)
    {
        _logger.LogDebug("Getting principal from token");

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtOptions.Secret);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtOptions.Audience,
                ValidateLifetime = false, // Don't validate lifetime when getting principal
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            return Task.FromResult(principal);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get principal from token");
            throw;
        }
    }

    public Task<bool> BlacklistTokenAsync(string token, DateTime expiration)
    {
        _logger.LogDebug("Blacklisting token");

        _blacklistedTokens[token] = expiration;

        // Clean up expired blacklisted tokens
        var expiredTokens = _blacklistedTokens
            .Where(kvp => kvp.Value < DateTime.UtcNow)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var expiredToken in expiredTokens)
        {
            _blacklistedTokens.TryRemove(expiredToken, out _);
        }

        return Task.FromResult(true);
    }

    public Task<bool> IsTokenBlacklistedAsync(string token)
    {
        _logger.LogDebug("Checking if token is blacklisted");

        var isBlacklisted = _blacklistedTokens.ContainsKey(token);
        return Task.FromResult(isBlacklisted);
    }

    public Task<RefreshToken?> GetRefreshTokenAsync(string token)
    {
        _logger.LogDebug("Getting refresh token");

        _refreshTokens.TryGetValue(token, out var refreshToken);
        return Task.FromResult(refreshToken);
    }

    public Task<bool> RevokeRefreshTokenAsync(string token, string ipAddress, string? reason = null)
    {
        _logger.LogDebug("Revoking refresh token");

        if (_refreshTokens.TryGetValue(token, out var refreshToken))
        {
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;
            refreshToken.ReasonRevoked = reason;
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public Task<bool> RevokeAllUserTokensAsync(string userId, string ipAddress, string? reason = null)
    {
        _logger.LogDebug("Revoking all tokens for user {UserId}", userId);

        var userTokens = _refreshTokens.Values
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow)
            .ToList();

        if (!userTokens.Any())
        {
            return Task.FromResult(false);
        }

        // Revoke all tokens
        foreach (var token in userTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
            token.ReasonRevoked = reason ?? "Revoked as part of user logout";
        }

        return Task.FromResult(true);
    }

    public Task<bool> RevokeTokenAsync(string token, string ipAddress, string? reason = null)
    {
        _logger.LogDebug("Revoking token");

        // For access tokens, we add them to the blacklist
        // Get token expiration from JWT
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            var expiration = jwtToken.ValidTo;

            // Blacklist the token
            _blacklistedTokens[token] = expiration;
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token");
            return Task.FromResult(false);
        }
    }
}
