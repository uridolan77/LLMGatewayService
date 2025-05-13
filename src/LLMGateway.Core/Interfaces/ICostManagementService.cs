using LLMGateway.Core.Models.Completion;
using LLMGateway.Core.Models.Cost;
using LLMGateway.Core.Models.Embedding;

namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Interface for cost management service
/// </summary>
public interface ICostManagementService
{
    /// <summary>
    /// Track completion cost
    /// </summary>
    /// <param name="request">Completion request</param>
    /// <param name="response">Completion response</param>
    /// <param name="userId">User ID</param>
    /// <param name="requestId">Request ID</param>
    /// <param name="projectId">Project ID</param>
    /// <param name="tags">Tags</param>
    /// <param name="metadata">Metadata</param>
    /// <returns>Cost record</returns>
    Task<CostRecord> TrackCompletionCostAsync(
        CompletionRequest request,
        CompletionResponse response,
        string userId,
        string requestId,
        string? projectId = null,
        IEnumerable<string>? tags = null,
        Dictionary<string, string>? metadata = null);
    
    /// <summary>
    /// Track embedding cost
    /// </summary>
    /// <param name="request">Embedding request</param>
    /// <param name="response">Embedding response</param>
    /// <param name="userId">User ID</param>
    /// <param name="requestId">Request ID</param>
    /// <param name="projectId">Project ID</param>
    /// <param name="tags">Tags</param>
    /// <param name="metadata">Metadata</param>
    /// <returns>Cost record</returns>
    Task<CostRecord> TrackEmbeddingCostAsync(
        EmbeddingRequest request,
        EmbeddingResponse response,
        string userId,
        string requestId,
        string? projectId = null,
        IEnumerable<string>? tags = null,
        Dictionary<string, string>? metadata = null);
    
    /// <summary>
    /// Track fine-tuning cost
    /// </summary>
    /// <param name="provider">Provider</param>
    /// <param name="modelId">Model ID</param>
    /// <param name="trainingTokens">Training tokens</param>
    /// <param name="userId">User ID</param>
    /// <param name="requestId">Request ID</param>
    /// <param name="projectId">Project ID</param>
    /// <param name="tags">Tags</param>
    /// <param name="metadata">Metadata</param>
    /// <returns>Cost record</returns>
    Task<CostRecord> TrackFineTuningCostAsync(
        string provider,
        string modelId,
        int trainingTokens,
        string userId,
        string requestId,
        string? projectId = null,
        IEnumerable<string>? tags = null,
        Dictionary<string, string>? metadata = null);
    
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
    /// Get cost report
    /// </summary>
    /// <param name="request">Report request</param>
    /// <param name="userId">User ID</param>
    /// <returns>Cost report</returns>
    Task<CostReport> GetCostReportAsync(CostReportRequest request, string userId);
    
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
    /// <param name="userId">User ID</param>
    /// <returns>Budget</returns>
    Task<Budget> GetBudgetAsync(string budgetId, string userId);
    
    /// <summary>
    /// Create budget
    /// </summary>
    /// <param name="request">Create request</param>
    /// <param name="userId">User ID</param>
    /// <returns>Created budget</returns>
    Task<Budget> CreateBudgetAsync(CreateBudgetRequest request, string userId);
    
    /// <summary>
    /// Update budget
    /// </summary>
    /// <param name="budgetId">Budget ID</param>
    /// <param name="request">Update request</param>
    /// <param name="userId">User ID</param>
    /// <returns>Updated budget</returns>
    Task<Budget> UpdateBudgetAsync(string budgetId, UpdateBudgetRequest request, string userId);
    
    /// <summary>
    /// Delete budget
    /// </summary>
    /// <param name="budgetId">Budget ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Task</returns>
    Task DeleteBudgetAsync(string budgetId, string userId);
    
    /// <summary>
    /// Get budget usage
    /// </summary>
    /// <param name="budgetId">Budget ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Budget usage</returns>
    Task<BudgetUsage> GetBudgetUsageAsync(string budgetId, string userId);
    
    /// <summary>
    /// Get all budget usages
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Budget usages</returns>
    Task<IEnumerable<BudgetUsage>> GetAllBudgetUsagesAsync(string userId);
    
    /// <summary>
    /// Check if operation is within budget
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="projectId">Project ID</param>
    /// <param name="estimatedCostUsd">Estimated cost in USD</param>
    /// <returns>True if within budget</returns>
    Task<bool> IsWithinBudgetAsync(string userId, string? projectId, decimal estimatedCostUsd);
    
    /// <summary>
    /// Get model pricing
    /// </summary>
    /// <param name="provider">Provider</param>
    /// <param name="modelId">Model ID</param>
    /// <returns>Model pricing</returns>
    Task<(decimal InputPricePerToken, decimal OutputPricePerToken)> GetModelPricingAsync(string provider, string modelId);
    
    /// <summary>
    /// Estimate completion cost
    /// </summary>
    /// <param name="provider">Provider</param>
    /// <param name="modelId">Model ID</param>
    /// <param name="inputTokens">Input tokens</param>
    /// <param name="outputTokens">Output tokens</param>
    /// <returns>Estimated cost in USD</returns>
    Task<decimal> EstimateCompletionCostAsync(string provider, string modelId, int inputTokens, int outputTokens);
    
    /// <summary>
    /// Estimate embedding cost
    /// </summary>
    /// <param name="provider">Provider</param>
    /// <param name="modelId">Model ID</param>
    /// <param name="inputTokens">Input tokens</param>
    /// <returns>Estimated cost in USD</returns>
    Task<decimal> EstimateEmbeddingCostAsync(string provider, string modelId, int inputTokens);
    
    /// <summary>
    /// Estimate fine-tuning cost
    /// </summary>
    /// <param name="provider">Provider</param>
    /// <param name="modelId">Model ID</param>
    /// <param name="trainingTokens">Training tokens</param>
    /// <returns>Estimated cost in USD</returns>
    Task<decimal> EstimateFineTuningCostAsync(string provider, string modelId, int trainingTokens);
}
