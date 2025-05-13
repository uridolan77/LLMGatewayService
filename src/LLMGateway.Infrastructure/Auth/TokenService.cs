using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Auth;
using LLMGateway.Core.Options;
using LLMGateway.Infrastructure.Persistence;
using LLMGateway.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace LLMGateway.Infrastructure.Auth;

/// <summary>
/// Token service
/// </summary>
public class TokenService : ITokenService
{
    private readonly JwtOptions _jwtOptions;
    private readonly LLMGatewayDbContext _dbContext;
    private readonly ILogger<TokenService> _logger;
    
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="jwtOptions">JWT options</param>
    /// <param name="dbContext">Database context</param>
    /// <param name="logger">Logger</param>
    public TokenService(
        IOptions<JwtOptions> jwtOptions,
        LLMGatewayDbContext dbContext,
        ILogger<TokenService> logger)
    {
        _jwtOptions = jwtOptions.Value;
        _dbContext = dbContext;
        _logger = logger;
    }
    
    /// <inheritdoc/>
    public async Task<string> GenerateAccessTokenAsync(Core.Models.Auth.User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtOptions.Secret);
        
        // Get user permissions
        var userEntity = await _dbContext.Users
            .Include(u => u.Permissions)
            .FirstOrDefaultAsync(u => u.Id == user.Id);
        
        if (userEntity == null)
        {
            throw new ArgumentException($"User with ID {user.Id} not found");
        }
        
        // Create claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, userEntity.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        
        // Add permission claims
        foreach (var permission in userEntity.Permissions.Where(p => p.IsGranted))
        {
            claims.Add(new Claim("llm-permissions", permission.Permission));
        }
        
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
        return tokenHandler.WriteToken(token);
    }
    
    /// <inheritdoc/>
    public async Task<Core.Models.Auth.RefreshToken> GenerateRefreshTokenAsync(string userId, string ipAddress)
    {        // Generate random token
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var refreshToken = Convert.ToBase64String(randomBytes);
        
        // Create refresh token entity
        var refreshTokenEntity = new Persistence.Entities.RefreshToken
        {
            UserId = userId,
            Token = refreshToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7), // 7 days expiry for refresh tokens
            CreatedByIp = ipAddress
        };
        
        // Save to database
        await _dbContext.RefreshTokens.AddAsync(refreshTokenEntity);
        await _dbContext.SaveChangesAsync();
        
        // Return as core model
        return new Core.Models.Auth.RefreshToken
        {
            Id = refreshTokenEntity.Id,
            UserId = refreshTokenEntity.UserId,
            Token = refreshTokenEntity.Token,
            ExpiresAt = refreshTokenEntity.ExpiresAt.DateTime,
            CreatedAt = refreshTokenEntity.CreatedAt.DateTime,
            CreatedByIp = refreshTokenEntity.CreatedByIp
        };
    }
    
    /// <inheritdoc/>
    public async Task<bool> ValidateTokenAsync(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtOptions.Secret);
        
        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtOptions.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return false;
        }
    }
    
    /// <inheritdoc/>
    public async Task<ClaimsPrincipal> GetPrincipalFromTokenAsync(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtOptions.Secret);
        
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = _jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = _jwtOptions.Audience,
            // Don't validate lifetime here since we're just trying to extract the claims
            ValidateLifetime = false,
            ClockSkew = TimeSpan.Zero
        };
        
        try
        {
            var principal = tokenHandler.ValidateToken(
                token, 
                tokenValidationParameters,
                out _);
                
            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get principal from token");
            throw;
        }
    }
    
    /// <inheritdoc/>
    public async Task<Core.Models.Auth.RefreshToken?> GetRefreshTokenAsync(string token)
    {
        var refreshToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == token);
            
        if (refreshToken == null)
        {
            return null;
        }
        
        // Map to core model
        return new Core.Models.Auth.RefreshToken
        {
            Id = refreshToken.Id,
            UserId = refreshToken.UserId,
            Token = refreshToken.Token,
            ExpiresAt = refreshToken.ExpiresAt.DateTime,
            CreatedAt = refreshToken.CreatedAt.DateTime,
            RevokedAt = refreshToken.RevokedAt?.DateTime,
            CreatedByIp = refreshToken.CreatedByIp,
            RevokedByIp = refreshToken.RevokedByIp,
            ReasonRevoked = refreshToken.ReasonRevoked
        };
    }
    
    /// <inheritdoc/>
    public async Task<bool> RevokeTokenAsync(string token, string ipAddress, string? reason = null)
    {
        var refreshToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == token);
            
        if (refreshToken == null)
        {
            return false;
        }
        
        if (!refreshToken.IsActive)
        {
            return false;
        }
        
        // Revoke token
        refreshToken.RevokedAt = DateTimeOffset.UtcNow;
        refreshToken.RevokedByIp = ipAddress;
        refreshToken.ReasonRevoked = reason ?? "Revoked without a specific reason";
        
        // Save changes
        _dbContext.RefreshTokens.Update(refreshToken);
        await _dbContext.SaveChangesAsync();
        
        return true;
    }
    
    /// <inheritdoc/>
    public async Task<bool> RevokeAllUserTokensAsync(string userId, string ipAddress, string? reason = null)
    {
        var activeTokens = await _dbContext.RefreshTokens
            .Where(r => r.UserId == userId && r.RevokedAt == null && r.ExpiresAt > DateTimeOffset.UtcNow)
            .ToListAsync();
            
        if (!activeTokens.Any())
        {
            return false;
        }
        
        // Revoke all tokens
        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTimeOffset.UtcNow;
            token.RevokedByIp = ipAddress;
            token.ReasonRevoked = reason ?? "Revoked as part of user logout";
        }
        
        // Save changes
        _dbContext.RefreshTokens.UpdateRange(activeTokens);
        await _dbContext.SaveChangesAsync();
        
        return true;
    }
}
