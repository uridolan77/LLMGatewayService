using LLMGateway.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LLMGateway.Infrastructure.Persistence.Repositories;

/// <summary>
/// Interface for user permission repository
/// </summary>
public interface IUserPermissionRepository : IRepository<UserPermission>
{
    /// <summary>
    /// Get user permissions by user ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User permissions</returns>
    Task<IEnumerable<UserPermission>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get user permission by user ID and permission
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="permission">Permission</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User permission</returns>
    Task<UserPermission?> GetByUserIdAndPermissionAsync(string userId, string permission, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if user has permission
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="permission">Permission</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Whether the user has the permission</returns>
    Task<bool> HasPermissionAsync(string userId, string permission, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Grant permission to user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="permission">Permission</param>
    /// <param name="grantedBy">Granted by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User permission</returns>
    Task<UserPermission> GrantPermissionAsync(string userId, string permission, string? grantedBy = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Revoke permission from user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="permission">Permission</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task RevokePermissionAsync(string userId, string permission, CancellationToken cancellationToken = default);
}

/// <summary>
/// User permission repository implementation
/// </summary>
public class UserPermissionRepository : Repository<UserPermission>, IUserPermissionRepository
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="context">Database context</param>
    public UserPermissionRepository(LLMGatewayDbContext context) : base(context)
    {
    }
    
    /// <inheritdoc/>
    public async Task<IEnumerable<UserPermission>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(p => p.UserId == userId).ToListAsync(cancellationToken);
    }
    
    /// <inheritdoc/>
    public async Task<UserPermission?> GetByUserIdAndPermissionAsync(string userId, string permission, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(p => p.UserId == userId && p.Permission == permission, cancellationToken);
    }
    
    /// <inheritdoc/>
    public async Task<bool> HasPermissionAsync(string userId, string permission, CancellationToken cancellationToken = default)
    {
        var userPermission = await GetByUserIdAndPermissionAsync(userId, permission, cancellationToken);
        return userPermission != null && userPermission.IsGranted;
    }
    
    /// <inheritdoc/>
    public async Task<UserPermission> GrantPermissionAsync(string userId, string permission, string? grantedBy = null, CancellationToken cancellationToken = default)
    {
        var userPermission = await GetByUserIdAndPermissionAsync(userId, permission, cancellationToken);
        
        if (userPermission == null)
        {
            userPermission = new UserPermission
            {
                UserId = userId,
                Permission = permission,
                IsGranted = true,
                GrantedBy = grantedBy,
                GrantedAt = DateTimeOffset.UtcNow
            };
            
            await _dbSet.AddAsync(userPermission, cancellationToken);
        }
        else
        {
            userPermission.IsGranted = true;
            userPermission.GrantedBy = grantedBy;
            userPermission.GrantedAt = DateTimeOffset.UtcNow;
            
            _dbSet.Update(userPermission);
        }
        
        await _context.SaveChangesAsync(cancellationToken);
        return userPermission;
    }
    
    /// <inheritdoc/>
    public async Task RevokePermissionAsync(string userId, string permission, CancellationToken cancellationToken = default)
    {
        var userPermission = await GetByUserIdAndPermissionAsync(userId, permission, cancellationToken);
        
        if (userPermission != null)
        {
            // Two approaches: we can either delete the record or set IsGranted to false
            // Here we'll set IsGranted to false to keep a record of the permission being revoked
            userPermission.IsGranted = false;
            _dbSet.Update(userPermission);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
