using LLMGateway.Core.Exceptions;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Completion;
using LLMGateway.Core.Models.Cost;
using LLMGateway.Core.Models.Embedding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LLMGateway.Core.Services;

/// <summary>
/// Service for cost management
/// </summary>
public class CostManagementService : ICostManagementService
{
    private readonly ICostRepository _repository;
    private readonly IModelService _modelService;
    private readonly ILogger<CostManagementService> _logger;
    private readonly CostManagementOptions _options;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="repository">Cost repository</param>
    /// <param name="modelService">Model service</param>
    /// <param name="options">Cost management options</param>
    /// <param name="logger">Logger</param>
    public CostManagementService(
        ICostRepository repository,
        IModelService modelService,
        IOptions<CostManagementOptions> options,
        ILogger<CostManagementService> logger)
    {
        _repository = repository;
        _modelService = modelService;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<CostRecord> TrackCompletionCostAsync(
        CompletionRequest request,
        CompletionResponse response,
        string userId,
        string requestId,
        string? projectId = null,
        IEnumerable<string>? tags = null,
        Dictionary<string, string>? metadata = null)
    {
        try
        {
            // Get token usage from the response
            var inputTokens = response.Usage?.PromptTokens ?? 0;
            var outputTokens = response.Usage?.CompletionTokens ?? 0;
            var totalTokens = response.Usage?.TotalTokens ?? 0;

            // Calculate cost
            var costUsd = await EstimateCompletionCostAsync(response.Provider, response.Model, inputTokens, outputTokens);

            // Create cost record
            var record = new CostRecord
            {
                Id = Guid.NewGuid().ToString(),
                RequestId = requestId,
                UserId = userId,
                Provider = response.Provider,
                ModelId = response.Model,
                OperationType = "completion",
                Timestamp = DateTime.UtcNow,
                InputTokens = inputTokens,
                OutputTokens = outputTokens,
                TotalTokens = totalTokens,
                CostUsd = costUsd,
                ProjectId = projectId,
                Tags = tags?.ToList() ?? new List<string>(),
                Metadata = metadata ?? new Dictionary<string, string>()
            };

            // Save cost record
            return await _repository.CreateCostRecordAsync(record);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track completion cost for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<CostRecord> TrackEmbeddingCostAsync(
        EmbeddingRequest request,
        EmbeddingResponse response,
        string userId,
        string requestId,
        string? projectId = null,
        IEnumerable<string>? tags = null,
        Dictionary<string, string>? metadata = null)
    {
        try
        {
            // Get token usage from the response
            var inputTokens = response.Usage?.TotalTokens ?? 0;
            var outputTokens = 0;
            var totalTokens = inputTokens;

            // Calculate cost
            var costUsd = await EstimateEmbeddingCostAsync(response.Provider, response.Model, inputTokens);

            // Create cost record
            var record = new CostRecord
            {
                Id = Guid.NewGuid().ToString(),
                RequestId = requestId,
                UserId = userId,
                Provider = response.Provider,
                ModelId = response.Model,
                OperationType = "embedding",
                Timestamp = DateTime.UtcNow,
                InputTokens = inputTokens,
                OutputTokens = outputTokens,
                TotalTokens = totalTokens,
                CostUsd = costUsd,
                ProjectId = projectId,
                Tags = tags?.ToList() ?? new List<string>(),
                Metadata = metadata ?? new Dictionary<string, string>()
            };

            // Save cost record
            return await _repository.CreateCostRecordAsync(record);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track embedding cost for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<CostRecord> TrackFineTuningCostAsync(
        string provider,
        string modelId,
        int trainingTokens,
        string userId,
        string requestId,
        string? projectId = null,
        IEnumerable<string>? tags = null,
        Dictionary<string, string>? metadata = null)
    {
        try
        {
            // Calculate cost
            var costUsd = await EstimateFineTuningCostAsync(provider, modelId, trainingTokens);

            // Create cost record
            var record = new CostRecord
            {
                Id = Guid.NewGuid().ToString(),
                RequestId = requestId,
                UserId = userId,
                Provider = provider,
                ModelId = modelId,
                OperationType = "fine-tuning",
                Timestamp = DateTime.UtcNow,
                InputTokens = trainingTokens,
                OutputTokens = 0,
                TotalTokens = trainingTokens,
                CostUsd = costUsd,
                ProjectId = projectId,
                Tags = tags?.ToList() ?? new List<string>(),
                Metadata = metadata ?? new Dictionary<string, string>()
            };

            // Save cost record
            return await _repository.CreateCostRecordAsync(record);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track fine-tuning cost for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
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
        try
        {
            return await _repository.GetCostRecordsAsync(
                userId,
                startDate,
                endDate,
                provider,
                modelId,
                operationType,
                projectId,
                tags);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cost records for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<CostReport> GetCostReportAsync(CostReportRequest request, string userId)
    {
        try
        {
            // Get cost summary
            var summary = await _repository.GetCostSummaryAsync(
                userId,
                request.StartDate,
                request.EndDate,
                request.Provider,
                request.ModelId,
                request.OperationType,
                request.ProjectId,
                request.Tags,
                request.GroupBy);

            // Get total cost
            var (totalCostUsd, totalTokens) = await _repository.GetTotalCostAsync(
                userId,
                request.StartDate,
                request.EndDate,
                request.Provider,
                request.ModelId,
                request.OperationType,
                request.ProjectId,
                request.Tags);

            // Create cost breakdown
            var breakdown = new List<CostBreakdown>();
            foreach (var (key, costUsd, tokens) in summary)
            {
                var percentage = totalCostUsd > 0 ? (decimal)costUsd / totalCostUsd * 100 : 0;

                breakdown.Add(new CostBreakdown
                {
                    Key = key,
                    CostUsd = costUsd,
                    Tokens = tokens,
                    Percentage = percentage
                });
            }

            // Create cost report
            var report = new CostReport
            {
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Grouping = request.GroupBy,
                TotalCostUsd = totalCostUsd,
                TotalTokens = totalTokens,
                Breakdown = breakdown
            };

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cost report for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Budget>> GetAllBudgetsAsync(string userId)
    {
        try
        {
            return await _repository.GetAllBudgetsAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all budgets for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Budget> GetBudgetAsync(string budgetId, string userId)
    {
        try
        {
            var budget = await _repository.GetBudgetByIdAsync(budgetId);
            if (budget == null)
            {
                throw new NotFoundException($"Budget with ID {budgetId} not found");
            }

            // Check if the user has access to the budget
            if (budget.UserId != userId)
            {
                throw new ForbiddenException("You don't have access to this budget");
            }

            return budget;
        }
        catch (Exception ex) when (ex is not NotFoundException && ex is not ForbiddenException)
        {
            _logger.LogError(ex, "Failed to get budget {BudgetId} for user {UserId}", budgetId, userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Budget> CreateBudgetAsync(CreateBudgetRequest request, string userId)
    {
        try
        {
            // Validate the request
            ValidateCreateBudgetRequest(request);

            // Create the budget
            var budget = new Budget
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                Description = request.Description ?? string.Empty,
                UserId = userId,
                ProjectId = request.ProjectId,
                AmountUsd = request.AmountUsd,
                StartDate = request.StartDate ?? DateTime.UtcNow,
                EndDate = request.EndDate,
                ResetPeriod = request.ResetPeriod,
                AlertThresholdPercentage = request.AlertThresholdPercentage,
                EnforceBudget = request.EnforceBudget,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Tags = request.Tags ?? new List<string>()
            };

            return await _repository.CreateBudgetAsync(budget);
        }
        catch (Exception ex) when (ex is not ValidationException)
        {
            _logger.LogError(ex, "Failed to create budget for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Budget> UpdateBudgetAsync(string budgetId, UpdateBudgetRequest request, string userId)
    {
        try
        {
            // Get the existing budget
            var budget = await GetBudgetAsync(budgetId, userId);

            // Update the budget properties
            if (request.Name != null)
            {
                budget.Name = request.Name;
            }

            if (request.Description != null)
            {
                budget.Description = request.Description;
            }

            if (request.ProjectId != null)
            {
                budget.ProjectId = request.ProjectId;
            }

            if (request.AmountUsd.HasValue)
            {
                if (request.AmountUsd.Value <= 0)
                {
                    throw new ValidationException("Amount must be greater than zero");
                }

                budget.AmountUsd = request.AmountUsd.Value;
            }

            if (request.EndDate.HasValue)
            {
                budget.EndDate = request.EndDate;
            }

            if (request.ResetPeriod.HasValue)
            {
                budget.ResetPeriod = request.ResetPeriod.Value;
            }

            if (request.AlertThresholdPercentage.HasValue)
            {
                if (request.AlertThresholdPercentage.Value < 0 || request.AlertThresholdPercentage.Value > 100)
                {
                    throw new ValidationException("Alert threshold percentage must be between 0 and 100");
                }

                budget.AlertThresholdPercentage = request.AlertThresholdPercentage.Value;
            }

            if (request.EnforceBudget.HasValue)
            {
                budget.EnforceBudget = request.EnforceBudget.Value;
            }

            if (request.Tags != null)
            {
                budget.Tags = request.Tags;
            }

            budget.UpdatedAt = DateTime.UtcNow;

            return await _repository.UpdateBudgetAsync(budget);
        }
        catch (Exception ex) when (ex is not NotFoundException && ex is not ForbiddenException && ex is not ValidationException)
        {
            _logger.LogError(ex, "Failed to update budget {BudgetId} for user {UserId}", budgetId, userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteBudgetAsync(string budgetId, string userId)
    {
        try
        {
            // Get the existing budget
            var budget = await GetBudgetAsync(budgetId, userId);

            await _repository.DeleteBudgetAsync(budgetId);
        }
        catch (Exception ex) when (ex is not NotFoundException && ex is not ForbiddenException)
        {
            _logger.LogError(ex, "Failed to delete budget {BudgetId} for user {UserId}", budgetId, userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<BudgetUsage> GetBudgetUsageAsync(string budgetId, string userId)
    {
        try
        {
            // Get the budget
            var budget = await GetBudgetAsync(budgetId, userId);

            // Calculate the budget period
            var (periodStart, periodEnd) = CalculateBudgetPeriod(budget);

            // Get the total cost for the budget period
            var (totalCostUsd, _) = await _repository.GetTotalCostAsync(
                userId,
                periodStart,
                periodEnd,
                null,
                null,
                null,
                budget.ProjectId,
                null);

            // Calculate usage
            var usedAmountUsd = totalCostUsd;
            var remainingAmountUsd = budget.AmountUsd - usedAmountUsd;
            var usagePercentage = budget.AmountUsd > 0 ? usedAmountUsd / budget.AmountUsd * 100 : 0;

            // Calculate next reset date
            var nextResetDate = CalculateNextResetDate(budget);

            // Create budget usage
            var usage = new BudgetUsage
            {
                BudgetId = budget.Id,
                BudgetName = budget.Name,
                AmountUsd = budget.AmountUsd,
                UsedAmountUsd = usedAmountUsd,
                RemainingAmountUsd = remainingAmountUsd,
                UsagePercentage = usagePercentage,
                StartDate = periodStart,
                EndDate = periodEnd,
                ResetPeriod = budget.ResetPeriod,
                NextResetDate = nextResetDate,
                AlertThresholdPercentage = budget.AlertThresholdPercentage,
                EnforceBudget = budget.EnforceBudget,
                IsBudgetExceeded = usedAmountUsd >= budget.AmountUsd,
                IsAlertThresholdReached = usagePercentage >= budget.AlertThresholdPercentage
            };

            return usage;
        }
        catch (Exception ex) when (ex is not NotFoundException && ex is not ForbiddenException)
        {
            _logger.LogError(ex, "Failed to get budget usage for budget {BudgetId} and user {UserId}", budgetId, userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<BudgetUsage>> GetAllBudgetUsagesAsync(string userId)
    {
        try
        {
            // Get all budgets for the user
            var budgets = await _repository.GetAllBudgetsAsync(userId);

            // Get usage for each budget
            var usages = new List<BudgetUsage>();
            foreach (var budget in budgets)
            {
                try
                {
                    var usage = await GetBudgetUsageAsync(budget.Id, userId);
                    usages.Add(usage);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get budget usage for budget {BudgetId}", budget.Id);
                    // Continue with the next budget
                }
            }

            return usages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all budget usages for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsWithinBudgetAsync(string userId, string? projectId, decimal estimatedCostUsd)
    {
        try
        {
            // Get budgets for the user and project
            var budgets = await _repository.GetBudgetsForUserAndProjectAsync(userId, projectId);

            // Check if any budget is enforced and would be exceeded
            foreach (var budget in budgets)
            {
                if (budget.EnforceBudget)
                {
                    // Calculate the budget period
                    var (periodStart, periodEnd) = CalculateBudgetPeriod(budget);

                    // Get the total cost for the budget period
                    var (totalCostUsd, _) = await _repository.GetTotalCostAsync(
                        userId,
                        periodStart,
                        periodEnd,
                        null,
                        null,
                        null,
                        budget.ProjectId,
                        null);

                    // Check if the budget would be exceeded
                    if (totalCostUsd + estimatedCostUsd > budget.AmountUsd)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if operation is within budget for user {UserId}", userId);
            // In case of error, allow the operation
            return true;
        }
    }

    /// <inheritdoc/>
    public async Task<(decimal InputPricePerToken, decimal OutputPricePerToken)> GetModelPricingAsync(string provider, string modelId)
    {
        try
        {
            // Get the model
            var model = await _modelService.GetModelAsync(modelId);
            if (model == null)
            {
                throw new ModelNotFoundException(modelId);
            }

            // Check if the model has pricing information
            if (model.InputPricePerToken > 0 && model.OutputPricePerToken > 0)
            {
                return (model.InputPricePerToken, model.OutputPricePerToken);
            }

            // Use default pricing from options
            if (_options.DefaultPricing.TryGetValue(provider, out var providerPricing))
            {
                if (providerPricing.TryGetValue(modelId, out var modelPricing))
                {
                    return (modelPricing.InputPricePerToken, modelPricing.OutputPricePerToken);
                }
            }

            // Use fallback pricing
            return (_options.FallbackInputPricePerToken, _options.FallbackOutputPricePerToken);
        }
        catch (Exception ex) when (ex is not ModelNotFoundException)
        {
            _logger.LogError(ex, "Failed to get pricing for model {ModelId} from provider {Provider}", modelId, provider);
            // Use fallback pricing
            return (_options.FallbackInputPricePerToken, _options.FallbackOutputPricePerToken);
        }
    }

    /// <inheritdoc/>
    public async Task<decimal> EstimateCompletionCostAsync(string provider, string modelId, int inputTokens, int outputTokens)
    {
        try
        {
            var (inputPricePerToken, outputPricePerToken) = await GetModelPricingAsync(provider, modelId);

            var inputCost = inputTokens * inputPricePerToken / 1000;
            var outputCost = outputTokens * outputPricePerToken / 1000;

            return inputCost + outputCost;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to estimate completion cost for model {ModelId} from provider {Provider}", modelId, provider);
            // Use fallback pricing
            var inputCost = inputTokens * _options.FallbackInputPricePerToken / 1000;
            var outputCost = outputTokens * _options.FallbackOutputPricePerToken / 1000;

            return inputCost + outputCost;
        }
    }

    /// <inheritdoc/>
    public async Task<decimal> EstimateEmbeddingCostAsync(string provider, string modelId, int inputTokens)
    {
        try
        {
            var (inputPricePerToken, _) = await GetModelPricingAsync(provider, modelId);

            return inputTokens * inputPricePerToken / 1000;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to estimate embedding cost for model {ModelId} from provider {Provider}", modelId, provider);
            // Use fallback pricing
            return inputTokens * _options.FallbackInputPricePerToken / 1000;
        }
    }

    /// <inheritdoc/>
    public async Task<decimal> EstimateFineTuningCostAsync(string provider, string modelId, int trainingTokens)
    {
        try
        {
            // Fine-tuning pricing is different from completion pricing
            // Use default pricing from options
            if (_options.FineTuningPricing.TryGetValue(provider, out var providerPricing))
            {
                if (providerPricing.TryGetValue(modelId, out var modelPricing))
                {
                    return trainingTokens * modelPricing / 1000;
                }
            }

            // Use fallback pricing
            return trainingTokens * _options.FallbackFineTuningPricePerToken / 1000;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to estimate fine-tuning cost for model {ModelId} from provider {Provider}", modelId, provider);
            // Use fallback pricing
            return trainingTokens * _options.FallbackFineTuningPricePerToken / 1000;
        }
    }

    #region Helper methods

    private static void ValidateCreateBudgetRequest(CreateBudgetRequest request)
    {
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.Add("Name", "Budget name is required");
        }

        if (request.AmountUsd <= 0)
        {
            errors.Add("AmountUsd", "Amount must be greater than zero");
        }

        if (request.AlertThresholdPercentage < 0 || request.AlertThresholdPercentage > 100)
        {
            errors.Add("AlertThresholdPercentage", "Alert threshold percentage must be between 0 and 100");
        }

        if (request.EndDate.HasValue && request.EndDate.Value <= DateTime.UtcNow)
        {
            errors.Add("EndDate", "End date must be in the future");
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("Invalid budget request", errors);
        }
    }

    private static (DateTime Start, DateTime? End) CalculateBudgetPeriod(Budget budget)
    {
        var now = DateTime.UtcNow;

        // If the budget has no reset period, use the entire budget period
        if (budget.ResetPeriod == BudgetResetPeriod.Never)
        {
            return (budget.StartDate, budget.EndDate);
        }

        // Calculate the current period based on the reset period
        DateTime periodStart;

        switch (budget.ResetPeriod)
        {
            case BudgetResetPeriod.Daily:
                periodStart = now.Date;
                break;

            case BudgetResetPeriod.Weekly:
                // Start of the week (Monday)
                var daysToMonday = ((int)now.DayOfWeek - 1 + 7) % 7;
                periodStart = now.Date.AddDays(-daysToMonday);
                break;

            case BudgetResetPeriod.Monthly:
                // Start of the month
                periodStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                break;

            case BudgetResetPeriod.Quarterly:
                // Start of the quarter
                var quarter = (now.Month - 1) / 3;
                var startMonth = quarter * 3 + 1;
                periodStart = new DateTime(now.Year, startMonth, 1, 0, 0, 0, DateTimeKind.Utc);
                break;

            case BudgetResetPeriod.Yearly:
                // Start of the year
                periodStart = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                break;

            default:
                periodStart = budget.StartDate;
                break;
        }

        // If the budget start date is after the calculated period start, use the budget start date
        if (budget.StartDate > periodStart)
        {
            periodStart = budget.StartDate;
        }

        return (periodStart, budget.EndDate);
    }

    private static DateTime? CalculateNextResetDate(Budget budget)
    {
        var now = DateTime.UtcNow;

        // If the budget has no reset period, return null
        if (budget.ResetPeriod == BudgetResetPeriod.Never)
        {
            return null;
        }

        // Calculate the next reset date based on the reset period
        DateTime nextReset;

        switch (budget.ResetPeriod)
        {
            case BudgetResetPeriod.Daily:
                nextReset = now.Date.AddDays(1);
                break;

            case BudgetResetPeriod.Weekly:
                // Next Monday
                var daysToMonday = ((int)now.DayOfWeek - 1 + 7) % 7;
                nextReset = now.Date.AddDays(7 - daysToMonday);
                break;

            case BudgetResetPeriod.Monthly:
                // First day of next month
                nextReset = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
                break;

            case BudgetResetPeriod.Quarterly:
                // First day of next quarter
                var quarter = (now.Month - 1) / 3;
                var nextQuarterStartMonth = quarter * 3 + 4;
                var nextQuarterYear = now.Year;

                if (nextQuarterStartMonth > 12)
                {
                    nextQuarterStartMonth -= 12;
                    nextQuarterYear++;
                }

                nextReset = new DateTime(nextQuarterYear, nextQuarterStartMonth, 1, 0, 0, 0, DateTimeKind.Utc);
                break;

            case BudgetResetPeriod.Yearly:
                // First day of next year
                nextReset = new DateTime(now.Year + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                break;

            default:
                return null;
        }

        // If the budget has an end date and it's before the next reset date, return null
        if (budget.EndDate.HasValue && budget.EndDate.Value < nextReset)
        {
            return null;
        }

        return nextReset;
    }

    #endregion
}
