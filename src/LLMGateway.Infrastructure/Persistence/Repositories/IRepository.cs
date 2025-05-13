using System.Linq.Expressions;

namespace LLMGateway.Infrastructure.Persistence.Repositories;

/// <summary>
/// Interface for generic repository operations
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Get all entities
    /// </summary>
    /// <returns>All entities</returns>
    Task<IEnumerable<T>> GetAllAsync();
    
    /// <summary>
    /// Get entities by predicate
    /// </summary>
    /// <param name="predicate">Predicate</param>
    /// <returns>Filtered entities</returns>
    Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> predicate);
    
    /// <summary>
    /// Get entity by ID
    /// </summary>
    /// <param name="id">Entity ID</param>
    /// <returns>Entity</returns>
    Task<T?> GetByIdAsync(string id);
    
    /// <summary>
    /// Add entity
    /// </summary>
    /// <param name="entity">Entity</param>
    /// <returns>Added entity</returns>
    Task<T> AddAsync(T entity);
    
    /// <summary>
    /// Update entity
    /// </summary>
    /// <param name="entity">Entity</param>
    /// <returns>Task</returns>
    Task UpdateAsync(T entity);
    
    /// <summary>
    /// Delete entity
    /// </summary>
    /// <param name="entity">Entity</param>
    /// <returns>Task</returns>
    Task DeleteAsync(T entity);
    
    /// <summary>
    /// Delete entity by ID
    /// </summary>
    /// <param name="id">Entity ID</param>
    /// <returns>Task</returns>
    Task DeleteByIdAsync(string id);
    
    /// <summary>
    /// Check if entity exists
    /// </summary>
    /// <param name="predicate">Predicate</param>
    /// <returns>True if entity exists</returns>
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
    
    /// <summary>
    /// Count entities
    /// </summary>
    /// <param name="predicate">Predicate</param>
    /// <returns>Count</returns>
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
    
    /// <summary>
    /// Save changes
    /// </summary>
    /// <returns>Task</returns>
    Task SaveChangesAsync();
}
