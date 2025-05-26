using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Cost;
using LLMGateway.Infrastructure.Persistence;
using LLMGateway.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LLMGateway.Infrastructure.Repositories;

/// <summary>
/// Repository for cost management
/// </summary>
public class CostRepository : ICostRepository
{
    private readonly LLMGatewayDbContext _dbContext;
    private readonly ILogger<CostRepository> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="dbContext">Database context</param>
    /// <param name="logger">Logger</param>
    public CostRepository(
        LLMGatewayDbContext dbContext,
        ILogger<CostRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Core.Models.Cost.CostRecord> CreateCostRecordAsync(Core.Models.Cost.CostRecord record)
    {
        try
        {
            var entity = Persistence.Entities.CostRecord.FromDomainModel(record);

            _dbContext.CostRecords.Add(entity);
            await _dbContext.SaveChangesAsync();

            return entity.ToDomainModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create cost record {RecordId}", record.Id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Core.Models.Cost.CostRecord>> GetCostRecordsAsync(
        string userId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? provider = null,
        string? modelId = null,
        string? operationType = null,
        string? projectId = null,
        IEnumerable<string>? tags = null)
    {
        try
        {
            var query = _dbContext.CostRecords.AsQueryable();

            // Apply filters
            query = query.Where(r => r.UserId == userId);

            if (startDate.HasValue)
            {
                query = query.Where(r => r.Timestamp >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(r => r.Timestamp <= endDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(provider))
            {
                query = query.Where(r => r.Provider == provider);
            }

            if (!string.IsNullOrWhiteSpace(modelId))
            {
                query = query.Where(r => r.ModelId == modelId);
            }

            if (!string.IsNullOrWhiteSpace(operationType))
            {
                query = query.Where(r => r.OperationType == operationType);
            }

            if (!string.IsNullOrWhiteSpace(projectId))
            {
                query = query.Where(r => r.ProjectId == projectId);
            }

            if (tags != null && tags.Any())
            {
                var tagList = tags.ToList();

                // This is a simple approach - in a real implementation, you would use a more sophisticated
                // approach to search for tags in the JSON array
                query = query.Where(r => tagList.Any(tag => r.TagsJson.Contains(tag)));
            }

            var entities = await query
                .OrderByDescending(r => r.Timestamp)
                .ToListAsync();

            return entities.Select(e => e.ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cost records for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<(string Key, decimal CostUsd, int Tokens)>> GetCostSummaryAsync(
        string userId,
        DateTime startDate,
        DateTime endDate,
        string? provider = null,
        string? modelId = null,
        string? operationType = null,
        string? projectId = null,
        IEnumerable<string>? tags = null,
        string groupBy = "provider")
    {
        try
        {
            // Get cost records
            var records = await GetCostRecordsAsync(
                userId,
                startDate,
                endDate,
                provider,
                modelId,
                operationType,
                projectId,
                tags);

            // Group by the specified field
            var summary = new List<(string Key, decimal CostUsd, int Tokens)>();

            switch (groupBy.ToLowerInvariant())
            {
                case "provider":
                    summary = records
                        .GroupBy(r => r.Provider)
                        .Select(g => (g.Key, g.Sum(r => r.CostUsd), g.Sum(r => r.TotalTokens)))
                        .OrderByDescending(x => x.Item2)
                        .ToList();
                    break;

                case "model":
                    summary = records
                        .GroupBy(r => r.ModelId)
                        .Select(g => (g.Key, g.Sum(r => r.CostUsd), g.Sum(r => r.TotalTokens)))
                        .OrderByDescending(x => x.Item2)
                        .ToList();
                    break;

                case "operation":
                    summary = records
                        .GroupBy(r => r.OperationType)
                        .Select(g => (g.Key, g.Sum(r => r.CostUsd), g.Sum(r => r.TotalTokens)))
                        .OrderByDescending(x => x.Item2)
                        .ToList();
                    break;

                case "project":
                    summary = records
                        .GroupBy(r => r.ProjectId ?? "none")
                        .Select(g => (g.Key, g.Sum(r => r.CostUsd), g.Sum(r => r.TotalTokens)))
                        .OrderByDescending(x => x.Item2)
                        .ToList();
                    break;

                case "day":
                    summary = records
                        .GroupBy(r => r.Timestamp.Date)
                        .Select(g => (g.Key.ToString("yyyy-MM-dd"), g.Sum(r => r.CostUsd), g.Sum(r => r.TotalTokens)))
                        .OrderBy(x => x.Item1)
                        .ToList();
                    break;

                case "month":
                    summary = records
                        .GroupBy(r => new { r.Timestamp.Year, r.Timestamp.Month })
                        .Select(g => ($"{g.Key.Year}-{g.Key.Month:D2}", g.Sum(r => r.CostUsd), g.Sum(r => r.TotalTokens)))
                        .OrderBy(x => x.Item1)
                        .ToList();
                    break;

                default:
                    summary = records
                        .GroupBy(r => r.Provider)
                        .Select(g => (g.Key, g.Sum(r => r.CostUsd), g.Sum(r => r.TotalTokens)))
                        .OrderByDescending(x => x.Item2)
                        .ToList();
                    break;
            }

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cost summary for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<(decimal TotalCostUsd, int TotalTokens)> GetTotalCostAsync(
        string userId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? provider = null,
        string? modelId = null,
        string? operationType = null,
        string? projectId = null,
        IEnumerable<string>? tags = null)
    {
        try
        {
            // Get cost records
            var records = await GetCostRecordsAsync(
                userId,
                startDate,
                endDate,
                provider,
                modelId,
                operationType,
                projectId,
                tags);

            // Calculate totals
            var totalCostUsd = records.Sum(r => r.CostUsd);
            var totalTokens = records.Sum(r => r.TotalTokens);

            return (totalCostUsd, totalTokens);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get total cost for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Core.Models.Cost.Budget>> GetAllBudgetsAsync(string userId)
    {
        try
        {
            var entities = await _dbContext.Budgets
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return entities.Select(e => e.ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all budgets for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Core.Models.Cost.Budget?> GetBudgetByIdAsync(string budgetId)
    {
        try
        {
            var entity = await _dbContext.Budgets
                .FirstOrDefaultAsync(b => b.Id == budgetId);

            return entity?.ToDomainModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get budget {BudgetId}", budgetId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Core.Models.Cost.Budget> CreateBudgetAsync(Core.Models.Cost.Budget budget)
    {
        try
        {
            var entity = Persistence.Entities.Budget.FromDomainModel(budget);

            _dbContext.Budgets.Add(entity);
            await _dbContext.SaveChangesAsync();

            return entity.ToDomainModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create budget {BudgetId}", budget.Id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Core.Models.Cost.Budget> UpdateBudgetAsync(Core.Models.Cost.Budget budget)
    {
        try
        {
            var entity = await _dbContext.Budgets
                .FirstOrDefaultAsync(b => b.Id == budget.Id);

            if (entity == null)
            {
                throw new Exception($"Budget with ID {budget.Id} not found");
            }

            // Update the entity
            entity.Name = budget.Name;
            entity.Description = budget.Description;
            entity.ProjectId = budget.ProjectId;
            entity.AmountUsd = budget.AmountUsd;
            entity.EndDate = budget.EndDate;
            entity.ResetPeriod = budget.ResetPeriod;
            entity.AlertThresholdPercentage = budget.AlertThresholdPercentage;
            entity.EnforceBudget = budget.EnforceBudget;
            entity.UpdatedAt = budget.UpdatedAt;

            entity.SetTags(budget.Tags);

            _dbContext.Budgets.Update(entity);
            await _dbContext.SaveChangesAsync();

            return entity.ToDomainModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update budget {BudgetId}", budget.Id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteBudgetAsync(string budgetId)
    {
        try
        {
            var entity = await _dbContext.Budgets
                .FirstOrDefaultAsync(b => b.Id == budgetId);

            if (entity != null)
            {
                _dbContext.Budgets.Remove(entity);
                await _dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete budget {BudgetId}", budgetId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Core.Models.Cost.Budget>> GetBudgetsForUserAndProjectAsync(string userId, string? projectId)
    {
        try
        {
            var query = _dbContext.Budgets
                .Where(b => b.UserId == userId);

            if (projectId != null)
            {
                // Get budgets for the specific project or budgets with no project
                query = query.Where(b => b.ProjectId == projectId || b.ProjectId == null);
            }
            else
            {
                // Get budgets with no project
                query = query.Where(b => b.ProjectId == null);
            }

            var entities = await query.ToListAsync();

            return entities.Select(e => e.ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get budgets for user {UserId} and project {ProjectId}", userId, projectId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Core.Models.Cost.CostCenter> CreateCostCenterAsync(Core.Models.Cost.CostCenter costCenter)
    {
        try
        {
            // For now, return the same object as we don't have a CostCenter entity yet
            // In a real implementation, you would create the entity and save to database
            _logger.LogInformation("Creating cost center {CostCenterId} for user {UserId}", costCenter.Id, costCenter.OwnerUserId);

            // Simulate database save
            await Task.Delay(10);

            return costCenter;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create cost center {CostCenterId}", costCenter.Id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Core.Models.Cost.CostCenter>> GetCostCentersAsync(string userId)
    {
        try
        {
            // For now, return mock data as we don't have a CostCenter entity yet
            // In a real implementation, you would query the database
            _logger.LogInformation("Getting cost centers for user {UserId}", userId);

            await Task.Delay(10);

            return new List<Core.Models.Cost.CostCenter>
            {
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Development",
                    Description = "Development team cost center",
                    OwnerUserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Production",
                    Description = "Production environment cost center",
                    OwnerUserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cost centers for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Core.Models.Cost.CostAlert> CreateCostAlertAsync(Core.Models.Cost.CostAlert alert)
    {
        try
        {
            // For now, return the same object as we don't have a CostAlert entity yet
            // In a real implementation, you would create the entity and save to database
            _logger.LogInformation("Creating cost alert {AlertId} for user {UserId}", alert.Id, alert.UserId);

            // Simulate database save
            await Task.Delay(10);

            return alert;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create cost alert {AlertId}", alert.Id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Core.Models.Cost.CostAlert>> GetCostAlertsAsync(string userId)
    {
        try
        {
            // For now, return mock data as we don't have a CostAlert entity yet
            // In a real implementation, you would query the database
            _logger.LogInformation("Getting cost alerts for user {UserId}", userId);

            await Task.Delay(10);

            return new List<Core.Models.Cost.CostAlert>
            {
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Monthly Budget Alert",
                    Type = "percentage",
                    UserId = userId,
                    ThresholdPercentage = 80,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cost alerts for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Core.Models.Cost.CostAlert>> GetCostAlertsAsync(string userId, string status)
    {
        try
        {
            // For now, return filtered mock data
            _logger.LogInformation("Getting cost alerts for user {UserId} with status {Status}", userId, status);

            var allAlerts = await GetCostAlertsAsync(userId);

            return status.ToLowerInvariant() switch
            {
                "active" => allAlerts.Where(a => a.IsActive),
                "inactive" => allAlerts.Where(a => !a.IsActive),
                "true" => allAlerts.Where(a => a.IsActive),
                "false" => allAlerts.Where(a => !a.IsActive),
                _ => allAlerts
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cost alerts for user {UserId} with status {Status}", userId, status);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Core.Models.Cost.CostAlert?> GetCostAlertAsync(string alertId)
    {
        try
        {
            // For now, return mock data
            _logger.LogInformation("Getting cost alert {AlertId}", alertId);

            await Task.Delay(10);

            return new Core.Models.Cost.CostAlert
            {
                Id = alertId,
                Name = "Monthly Budget Alert",
                Type = "percentage",
                UserId = "user-123",
                ThresholdPercentage = 80,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cost alert {AlertId}", alertId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Core.Models.Cost.CostAlert> UpdateCostAlertAsync(Core.Models.Cost.CostAlert alert)
    {
        try
        {
            // For now, return the same object
            _logger.LogInformation("Updating cost alert {AlertId}", alert.Id);

            // Simulate database update
            await Task.Delay(10);
            // Note: CostAlert model doesn't have UpdatedAt property

            return alert;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update cost alert {AlertId}", alert.Id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteCostAlertAsync(string alertId)
    {
        try
        {
            // For now, just log the operation
            _logger.LogInformation("Deleting cost alert {AlertId}", alertId);

            // Simulate database delete
            await Task.Delay(10);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete cost alert {AlertId}", alertId);
            throw;
        }
    }
}
