using LLMGateway.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace LLMGateway.Infrastructure.Persistence.Repositories;

/// <summary>
/// Interface for user repository
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Get user by username
    /// </summary>
    /// <param name="username">Username</param>
    /// <returns>User</returns>
    Task<User?> GetByUsernameAsync(string username);
    
    /// <summary>
    /// Get user by email
    /// </summary>
    /// <param name="email">Email</param>
    /// <returns>User</returns>
    Task<User?> GetByEmailAsync(string email);
    
    /// <summary>
    /// Get users by role
    /// </summary>
    /// <param name="role">Role</param>
    /// <returns>Users</returns>
    Task<IEnumerable<User>> GetByRoleAsync(string role);
    
    /// <summary>
    /// Get user with API keys
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>User with API keys</returns>
    Task<User?> GetWithApiKeysAsync(string userId);
}

/// <summary>
/// User repository
/// </summary>
public class UserRepository : Repository<User>, IUserRepository
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context">Database context</param>
    public UserRepository(LLMGatewayDbContext context) : base(context)
    {
    }
    
    /// <inheritdoc/>
    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Username == username);
    }
    
    /// <inheritdoc/>
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
    }
    
    /// <inheritdoc/>
    public async Task<IEnumerable<User>> GetByRoleAsync(string role)
    {
        return await _dbSet.Where(u => u.Role == role).ToListAsync();
    }
    
    /// <inheritdoc/>
    public async Task<User?> GetWithApiKeysAsync(string userId)
    {
        return await _dbSet
            .Include(u => u.ApiKeys)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }
}
