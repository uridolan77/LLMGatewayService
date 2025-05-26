using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Cost;
using Microsoft.Extensions.Logging;

namespace LLMGateway.Core.Services;

/// <summary>
/// In-memory implementation of ICostRepository for when database is not available
/// </summary>
public class InMemoryCostRepository : ICostRepository
{
    private readonly ILogger<InMemoryCostRepository> _logger;
    private readonly List<CostRecord> _costRecords = new();
    private readonly List<CostCenter> _costCenters = new();
    private readonly List<CostAlert> _costAlerts = new();
    private readonly List<Budget> _budgets = new();

    public InMemoryCostRepository(ILogger<InMemoryCostRepository> logger)
    {
        _logger = logger;
    }

    public async Task<CostRecord> CreateCostRecordAsync(CostRecord costRecord)
    {
        _logger.LogInformation("Creating cost record for user {UserId}", costRecord.UserId);

        costRecord.Id = Guid.NewGuid().ToString();
        costRecord.Timestamp = DateTime.UtcNow;

        _costRecords.Add(costRecord);

        await Task.CompletedTask;
        return costRecord;
    }

    public async Task<IEnumerable<CostRecord>> GetCostRecordsAsync(
        string userId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? provider = null,
        string? modelId = null,
        string? operationType = null,
        string? projectId = null,
        IEnumerable<string>? tags = null)
    {
        _logger.LogInformation("Getting cost records for user {UserId}", userId);

        var query = _costRecords.Where(r => r.UserId == userId);

        if (startDate.HasValue)
            query = query.Where(r => r.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(r => r.Timestamp <= endDate.Value);

        if (!string.IsNullOrEmpty(provider))
            query = query.Where(r => r.Provider == provider);

        if (!string.IsNullOrEmpty(modelId))
            query = query.Where(r => r.ModelId == modelId);

        if (!string.IsNullOrEmpty(operationType))
            query = query.Where(r => r.OperationType == operationType);

        if (!string.IsNullOrEmpty(projectId))
            query = query.Where(r => r.ProjectId == projectId);

        await Task.CompletedTask;
        return query.ToList();
    }

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
        var records = await GetCostRecordsAsync(userId, startDate, endDate, provider, modelId, operationType, projectId, tags);

        var totalCost = records.Sum(r => r.CostUsd);
        var totalTokens = records.Sum(r => r.TotalTokens);

        return (totalCost, totalTokens);
    }

    public async Task<IEnumerable<CostRecord>> GetCostRecordsByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        string? userId = null,
        string? provider = null,
        string? modelId = null)
    {
        _logger.LogInformation("Getting cost records by date range {StartDate} to {EndDate}", startDate, endDate);

        var query = _costRecords.Where(r => r.Timestamp >= startDate && r.Timestamp <= endDate);

        if (!string.IsNullOrEmpty(userId))
            query = query.Where(r => r.UserId == userId);

        if (!string.IsNullOrEmpty(provider))
            query = query.Where(r => r.Provider == provider);

        if (!string.IsNullOrEmpty(modelId))
            query = query.Where(r => r.ModelId == modelId);

        await Task.CompletedTask;
        return query.ToList();
    }

    public async Task<CostCenter> CreateCostCenterAsync(CostCenter costCenter)
    {
        _logger.LogInformation("Creating cost center {CostCenterId}", costCenter.Id);

        costCenter.Id = Guid.NewGuid().ToString();
        costCenter.CreatedAt = DateTime.UtcNow;

        _costCenters.Add(costCenter);

        await Task.CompletedTask;
        return costCenter;
    }

    public async Task<IEnumerable<CostCenter>> GetCostCentersAsync(string userId)
    {
        _logger.LogInformation("Getting cost centers for user {UserId}", userId);

        var centers = _costCenters.Where(c => c.OwnerUserId == userId).ToList();

        await Task.CompletedTask;
        return centers;
    }

    public async Task<CostAlert> CreateCostAlertAsync(CostAlert alert)
    {
        _logger.LogInformation("Creating cost alert {AlertId}", alert.Id);

        alert.Id = Guid.NewGuid().ToString();
        alert.CreatedAt = DateTime.UtcNow;

        _costAlerts.Add(alert);

        await Task.CompletedTask;
        return alert;
    }

    public async Task<IEnumerable<CostAlert>> GetCostAlertsAsync(string userId)
    {
        _logger.LogInformation("Getting cost alerts for user {UserId}", userId);

        var alerts = _costAlerts.Where(a => a.UserId == userId).ToList();

        await Task.CompletedTask;
        return alerts;
    }

    public async Task<CostAlert?> GetCostAlertAsync(string alertId)
    {
        _logger.LogInformation("Getting cost alert {AlertId}", alertId);

        var alert = _costAlerts.FirstOrDefault(a => a.Id == alertId);

        await Task.CompletedTask;
        return alert;
    }

    public async Task<CostAlert> UpdateCostAlertAsync(CostAlert alert)
    {
        _logger.LogInformation("Updating cost alert {AlertId}", alert.Id);

        var existingAlert = _costAlerts.FirstOrDefault(a => a.Id == alert.Id);
        if (existingAlert != null)
        {
            _costAlerts.Remove(existingAlert);
            _costAlerts.Add(alert);
        }

        await Task.CompletedTask;
        return alert;
    }

    public async Task DeleteCostAlertAsync(string alertId)
    {
        _logger.LogInformation("Deleting cost alert {AlertId}", alertId);

        var alert = _costAlerts.FirstOrDefault(a => a.Id == alertId);
        if (alert != null)
        {
            _costAlerts.Remove(alert);
        }

        await Task.CompletedTask;
    }

    public async Task<IEnumerable<CostRecord>> GetCostRecordsByProviderAsync(
        string provider,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        _logger.LogInformation("Getting cost records for provider {Provider}", provider);

        var query = _costRecords.Where(r => r.Provider == provider);

        if (startDate.HasValue)
            query = query.Where(r => r.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(r => r.Timestamp <= endDate.Value);

        await Task.CompletedTask;
        return query.ToList();
    }

    public async Task<IEnumerable<CostRecord>> GetCostRecordsByModelAsync(
        string modelId,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        _logger.LogInformation("Getting cost records for model {ModelId}", modelId);

        var query = _costRecords.Where(r => r.ModelId == modelId);

        if (startDate.HasValue)
            query = query.Where(r => r.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(r => r.Timestamp <= endDate.Value);

        await Task.CompletedTask;
        return query.ToList();
    }

    public async Task<IEnumerable<CostRecord>> GetCostRecordsByProjectAsync(
        string projectId,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        _logger.LogInformation("Getting cost records for project {ProjectId}", projectId);

        var query = _costRecords.Where(r => r.ProjectId == projectId);

        if (startDate.HasValue)
            query = query.Where(r => r.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(r => r.Timestamp <= endDate.Value);

        await Task.CompletedTask;
        return query.ToList();
    }

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
        _logger.LogInformation("Getting cost summary for user {UserId}", userId);

        var records = await GetCostRecordsAsync(userId, startDate, endDate, provider, modelId, operationType, projectId, tags);

        var summary = new List<(string Key, decimal CostUsd, int Tokens)>();

        switch (groupBy.ToLowerInvariant())
        {
            case "provider":
                summary = records.GroupBy(r => r.Provider)
                    .Select(g => (g.Key, g.Sum(r => r.CostUsd), g.Sum(r => r.TotalTokens)))
                    .ToList();
                break;
            case "model":
                summary = records.GroupBy(r => r.ModelId)
                    .Select(g => (g.Key, g.Sum(r => r.CostUsd), g.Sum(r => r.TotalTokens)))
                    .ToList();
                break;
            case "operation":
                summary = records.GroupBy(r => r.OperationType)
                    .Select(g => (g.Key, g.Sum(r => r.CostUsd), g.Sum(r => r.TotalTokens)))
                    .ToList();
                break;
            default:
                summary = new List<(string Key, decimal CostUsd, int Tokens)>
                {
                    ("Total", records.Sum(r => r.CostUsd), records.Sum(r => r.TotalTokens))
                };
                break;
        }

        return summary;
    }

    public async Task<Budget> CreateBudgetAsync(Budget budget)
    {
        _logger.LogInformation("Creating budget {BudgetId}", budget.Id);

        budget.Id = Guid.NewGuid().ToString();
        budget.CreatedAt = DateTime.UtcNow;

        _budgets.Add(budget);

        await Task.CompletedTask;
        return budget;
    }

    public async Task<IEnumerable<Budget>> GetAllBudgetsAsync(string userId)
    {
        _logger.LogInformation("Getting all budgets for user {UserId}", userId);

        var budgets = _budgets.Where(b => b.UserId == userId).ToList();

        await Task.CompletedTask;
        return budgets;
    }

    public async Task<Budget?> GetBudgetByIdAsync(string budgetId)
    {
        _logger.LogInformation("Getting budget {BudgetId}", budgetId);

        var budget = _budgets.FirstOrDefault(b => b.Id == budgetId);

        await Task.CompletedTask;
        return budget;
    }

    public async Task<Budget> UpdateBudgetAsync(Budget budget)
    {
        _logger.LogInformation("Updating budget {BudgetId}", budget.Id);

        var existingBudget = _budgets.FirstOrDefault(b => b.Id == budget.Id);
        if (existingBudget != null)
        {
            _budgets.Remove(existingBudget);
            _budgets.Add(budget);
        }

        await Task.CompletedTask;
        return budget;
    }

    public async Task DeleteBudgetAsync(string budgetId)
    {
        _logger.LogInformation("Deleting budget {BudgetId}", budgetId);

        var budget = _budgets.FirstOrDefault(b => b.Id == budgetId);
        if (budget != null)
        {
            _budgets.Remove(budget);
        }

        await Task.CompletedTask;
    }

    public async Task<IEnumerable<Budget>> GetBudgetsForUserAndProjectAsync(string userId, string? projectId)
    {
        _logger.LogInformation("Getting budgets for user {UserId} and project {ProjectId}", userId, projectId);

        var query = _budgets.Where(b => b.UserId == userId);

        if (!string.IsNullOrEmpty(projectId))
        {
            query = query.Where(b => b.ProjectId == projectId);
        }

        await Task.CompletedTask;
        return query.ToList();
    }

    public async Task<IEnumerable<CostAlert>> GetCostAlertsAsync(string userId, string status)
    {
        _logger.LogInformation("Getting cost alerts for user {UserId} with status {Status}", userId, status);

        // Since CostAlert doesn't have a Status property, we'll filter by IsActive instead
        // This is a simplified implementation for the in-memory repository
        var isActive = status.ToLowerInvariant() == "active";
        var alerts = _costAlerts.Where(a => a.UserId == userId && a.IsActive == isActive).ToList();

        await Task.CompletedTask;
        return alerts;
    }
}
