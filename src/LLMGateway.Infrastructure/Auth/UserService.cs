using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Auth;
using LLMGateway.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace LLMGateway.Infrastructure.Auth;

/// <summary>
/// User service implementation
/// </summary>
public class UserService : IUserService
{
    private readonly LLMGatewayDbContext _dbContext;
    private readonly ILogger<UserService> _logger;
    
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="dbContext">Database context</param>
    /// <param name="logger">Logger</param>
    public UserService(
        LLMGatewayDbContext dbContext,
        ILogger<UserService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    /// <inheritdoc/>
    public async Task<User?> GetByIdAsync(string id)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == id);
            
        return user == null ? null : MapToCore(user);
    }
    
    /// <inheritdoc/>
    public async Task<User?> GetByUsernameAsync(string username)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == username);
            
        return user == null ? null : MapToCore(user);
    }
    
    /// <inheritdoc/>
    public async Task<User?> GetByEmailAsync(string email)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == email);
            
        return user == null ? null : MapToCore(user);
    }
    
    /// <inheritdoc/>
    public async Task<User> CreateAsync(User user, string password)
    {
        // Check if username already exists
        if (await _dbContext.Users.AnyAsync(u => u.Username == user.Username))
        {
            throw new InvalidOperationException($"Username '{user.Username}' is already taken");
        }
        
        // Check if email already exists
        if (await _dbContext.Users.AnyAsync(u => u.Email == user.Email))
        {
            throw new InvalidOperationException($"Email '{user.Email}' is already registered");
        }
        
        // Create password hash
        var passwordHash = HashPassword(password);
          // Create user entity
        var userEntity = new Persistence.Entities.User
        {
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PasswordHash = passwordHash,
            Role = user.Roles.FirstOrDefault() ?? "User",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        
        // Save user
        await _dbContext.Users.AddAsync(userEntity);
        await _dbContext.SaveChangesAsync();
        
        // Return created user
        return MapToCore(userEntity);
    }
    
    /// <inheritdoc/>
    public async Task<bool> UpdateAsync(User user)
    {
        var userEntity = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == user.Id);
            
        if (userEntity == null)
        {
            return false;
        }
        
        // Check if username is changed and already exists
        if (userEntity.Username != user.Username && 
            await _dbContext.Users.AnyAsync(u => u.Username == user.Username && u.Id != user.Id))
        {
            throw new InvalidOperationException($"Username '{user.Username}' is already taken");
        }
        
        // Check if email is changed and already exists
        if (userEntity.Email != user.Email && 
            await _dbContext.Users.AnyAsync(u => u.Email == user.Email && u.Id != user.Id))
        {
            throw new InvalidOperationException($"Email '{user.Email}' is already registered");
        }
          // Update user properties
        userEntity.Username = user.Username;
        userEntity.Email = user.Email;
        userEntity.FirstName = user.FirstName;
        userEntity.LastName = user.LastName;
        userEntity.IsActive = user.IsActive;
        userEntity.Role = user.Roles.FirstOrDefault() ?? userEntity.Role;
        
        // Save changes
        _dbContext.Users.Update(userEntity);
        await _dbContext.SaveChangesAsync();
        
        return true;
    }
    
    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(string id)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == id);
            
        if (user == null)
        {
            return false;
        }
        
        // Instead of hard delete, mark user as inactive
        user.IsActive = false;
        
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();
        
        return true;
    }
    
    /// <inheritdoc/>
    public async Task<bool> VerifyPasswordAsync(User user, string password)
    {
        var userEntity = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == user.Id);
            
        if (userEntity == null || userEntity.PasswordHash == null)
        {
            return false;
        }
        
        return VerifyPasswordHash(password, userEntity.PasswordHash);
    }
    
    /// <inheritdoc/>
    public async Task<bool> UpdatePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
            
        if (user == null || user.PasswordHash == null)
        {
            return false;
        }
        
        // Verify current password
        if (!VerifyPasswordHash(currentPassword, user.PasswordHash))
        {
            return false;
        }
        
        // Create new password hash
        user.PasswordHash = HashPassword(newPassword);
        
        // Save changes
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();
        
        return true;
    }
    
    /// <inheritdoc/>
    public async Task<List<User>> GetAllAsync(int skip = 0, int take = 100)
    {
        var users = await _dbContext.Users
            .OrderBy(u => u.Username)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
            
        return users.Select(MapToCore).ToList();
    }
      /// <inheritdoc/>
    public async Task<List<User>> GetUsersInRoleAsync(string role, int skip = 0, int take = 100)
    {
        var users = await _dbContext.Users
            .Where(u => u.Role == role)
            .OrderBy(u => u.Username)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
            
        return users.Select(MapToCore).ToList();
    }
      /// <inheritdoc/>
    public async Task<bool> AddToRoleAsync(string userId, string role)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
            
        if (user == null)
        {
            return false;
        }
        
        user.Role = role;
        
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();
        
        return true;
    }
      /// <inheritdoc/>
    public async Task<bool> RemoveFromRoleAsync(string userId, string role)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
            
        if (user == null || user.Role != role)
        {
            return false;
        }
        
        // Reset to default role
        user.Role = "User";
        
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();
        
        return true;
    }
      /// <inheritdoc/>
    public async Task<bool> IsInRoleAsync(string userId, string role)
    {
        return await _dbContext.Users
            .AnyAsync(u => u.Id == userId && u.Role == role);
    }
    
    #region Private methods
      /// <summary>
    /// Map user entity to core model
    /// </summary>
    /// <param name="entity">User entity</param>
    /// <returns>User core model</returns>
    private static User MapToCore(Persistence.Entities.User entity)
    {
        return new User
        {
            Id = entity.Id,
            Username = entity.Username,
            Email = entity.Email,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            Roles = new List<string> { entity.Role },
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt.DateTime,
            LastLogin = entity.LastLoginAt?.DateTime
        };
    }
    
    /// <summary>
    /// Hash password
    /// </summary>
    /// <param name="password">Password</param>
    /// <returns>Hashed password</returns>
    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
    
    /// <summary>
    /// Verify password hash
    /// </summary>
    /// <param name="password">Password</param>
    /// <param name="passwordHash">Password hash</param>
    /// <returns>Whether the password is valid</returns>
    private static bool VerifyPasswordHash(string password, string passwordHash)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        var hash = Convert.ToBase64String(hashedBytes);
        
        return hash == passwordHash;
    }
    
    #endregion
}
