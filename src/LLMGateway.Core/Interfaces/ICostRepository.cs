using LLMGateway.Core.Models.Cost;

namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Interface for cost repository
/// </summary>
public interface ICostRepository
{
    /// <summary>
    /// Create cost record
    /// </summary>
    /// <param name="record">Cost record</param>
    /// <returns>Created cost record</returns>
    Task<CostRecord> CreateCostRecordAsync(CostRecord record);

    /// <summary>
    /// Get cost records
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="provider">Provider</param>
    /// <param name="modelId">Model ID</param>
    /// <param name="operationType">Operation type</param>
    /// <param name="projectId">Project ID</param>
    /// <param name="tags">Tags</param>
    /// <returns>Cost records</returns>
    Task<IEnumerable<CostRecord>> GetCostRecordsAsync(
        string userId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? provider = null,
        string? modelId = null,
        string? operationType = null,
        string? projectId = null,
        IEnumerable<string>? tags = null);

    /// <summary>
    /// Get cost summary
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="provider">Provider</param>
    /// <param name="modelId">Model ID</param>
    /// <param name="operationType">Operation type</param>
    /// <param name="projectId">Project ID</param>
    /// <param name="tags">Tags</param>
    /// <param name="groupBy">Group by</param>
    /// <returns>Cost summary</returns>
    Task<IEnumerable<(string Key, decimal CostUsd, int Tokens)>> GetCostSummaryAsync(
        string userId,
        DateTime startDate,
        DateTime endDate,
        string? provider = null,
        string? modelId = null,
        string? operationType = null,
        string? projectId = null,
        IEnumerable<string>? tags = null,
        string groupBy = "provider");

    /// <summary>
    /// Get total cost
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="provider">Provider</param>
    /// <param name="modelId">Model ID</param>
    /// <param name="operationType">Operation type</param>
    /// <param name="projectId">Project ID</param>
    /// <param name="tags">Tags</param>
    /// <returns>Total cost and tokens</returns>
    Task<(decimal TotalCostUsd, int TotalTokens)> GetTotalCostAsync(
        string userId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? provider = null,
        string? modelId = null,
        string? operationType = null,
        string? projectId = null,
        IEnumerable<string>? tags = null);

    /// <summary>
    /// Get all budgets
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Budgets</returns>
    Task<IEnumerable<Budget>> GetAllBudgetsAsync(string userId);

    /// <summary>
    /// Get budget by ID
    /// </summary>
    /// <param name="budgetId">Budget ID</param>
    /// <returns>Budget</returns>
    Task<Budget?> GetBudgetByIdAsync(string budgetId);

    /// <summary>
    /// Create budget
    /// </summary>
    /// <param name="budget">Budget</param>
    /// <returns>Created budget</returns>
    Task<Budget> CreateBudgetAsync(Budget budget);

    /// <summary>
    /// Update budget
    /// </summary>
    /// <param name="budget">Budget</param>
    /// <returns>Updated budget</returns>
    Task<Budget> UpdateBudgetAsync(Budget budget);

    /// <summary>
    /// Delete budget
    /// </summary>
    /// <param name="budgetId">Budget ID</param>
    /// <returns>Task</returns>
    Task DeleteBudgetAsync(string budgetId);

    /// <summary>
    /// Get budgets for user and project
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="projectId">Project ID</param>
    /// <returns>Budgets</returns>
    Task<IEnumerable<Budget>> GetBudgetsForUserAndProjectAsync(string userId, string? projectId);

    /// <summary>
    /// Create cost center
    /// </summary>
    /// <param name="costCenter">Cost center</param>
    /// <returns>Created cost center</returns>
    Task<CostCenter> CreateCostCenterAsync(CostCenter costCenter);

    /// <summary>
    /// Get cost centers for user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Cost centers</returns>
    Task<IEnumerable<CostCenter>> GetCostCentersAsync(string userId);

    /// <summary>
    /// Create cost alert
    /// </summary>
    /// <param name="alert">Cost alert</param>
    /// <returns>Created cost alert</returns>
    Task<CostAlert> CreateCostAlertAsync(CostAlert alert);

    /// <summary>
    /// Get cost alerts for user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Cost alerts</returns>
    Task<IEnumerable<CostAlert>> GetCostAlertsAsync(string userId);

    /// <summary>
    /// Get cost alerts for user with status filter
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="status">Alert status</param>
    /// <returns>Cost alerts</returns>
    Task<IEnumerable<CostAlert>> GetCostAlertsAsync(string userId, string status);

    /// <summary>
    /// Get cost alert by ID
    /// </summary>
    /// <param name="alertId">Alert ID</param>
    /// <returns>Cost alert</returns>
    Task<CostAlert?> GetCostAlertAsync(string alertId);

    /// <summary>
    /// Update cost alert
    /// </summary>
    /// <param name="alert">Cost alert</param>
    /// <returns>Updated cost alert</returns>
    Task<CostAlert> UpdateCostAlertAsync(CostAlert alert);

    /// <summary>
    /// Delete cost alert
    /// </summary>
    /// <param name="alertId">Alert ID</param>
    /// <returns>Task</returns>
    Task DeleteCostAlertAsync(string alertId);
}
