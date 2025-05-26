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
                var percentage = totalCostUsd > 0 ? (double)((decimal)costUsd / totalCostUsd * 100) : 0;

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

    /// <inheritdoc/>
    public async Task<AdvancedCostAnalytics> GetAdvancedCostAnalyticsAsync(AdvancedCostAnalyticsRequest request, string userId)
    {
        try
        {
            _logger.LogInformation("Getting advanced cost analytics for user {UserId}", userId);

            // This would typically query a comprehensive analytics database
            // For now, return a simulated response
            var analytics = new AdvancedCostAnalytics
            {
                TotalCost = 1250.75m,
                Currency = request.Currency,
                Trends = new CostTrends
                {
                    PeriodOverPeriodChange = 125.50m,
                    PeriodOverPeriodPercentageChange = 11.2,
                    TrendDirection = "increasing",
                    AverageDailyCost = 41.69m,
                    PeakCostDay = DateTime.UtcNow.AddDays(-5),
                    PeakCostAmount = 89.45m,
                    CostVolatility = 15.3
                },
                BreakdownByDimension = new Dictionary<string, CostBreakdown>
                {
                    ["provider"] = new CostBreakdown
                    {
                        TotalCost = 1250.75m,
                        Currency = request.Currency,
                        BreakdownByDimension = new Dictionary<string, List<CostBreakdownItem>>
                        {
                            ["provider"] = new List<CostBreakdownItem>
                            {
                                new() { DimensionValue = "OpenAI", Cost = 750.45m, PercentageOfTotal = 60.0 },
                                new() { DimensionValue = "Anthropic", Cost = 350.20m, PercentageOfTotal = 28.0 },
                                new() { DimensionValue = "Cohere", Cost = 150.10m, PercentageOfTotal = 12.0 }
                            }
                        }
                    }
                },
                EfficiencyMetrics = new CostEfficiencyMetrics
                {
                    CostPerRequest = 0.125m,
                    CostPerToken = 0.00002m,
                    CostPerSuccessfulRequest = 0.127m,
                    EfficiencyScore = 85.5
                }
            };

            if (request.IncludeForecast)
            {
                analytics.Forecast = new CostForecast
                {
                    ForecastStart = DateTime.UtcNow,
                    ForecastEnd = DateTime.UtcNow.AddDays(30),
                    PredictedTotalCost = 1400.00m,
                    ConfidenceLevel = 0.85,
                    Methodology = "Linear regression with seasonal adjustments"
                };
            }

            return analytics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get advanced cost analytics for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<CostOptimizationRecommendations> GetCostOptimizationRecommendationsAsync(string userId, TimeSpan period)
    {
        try
        {
            _logger.LogInformation("Getting cost optimization recommendations for user {UserId}", userId);

            // This would typically analyze usage patterns and costs
            // For now, return simulated recommendations
            var recommendations = new CostOptimizationRecommendations
            {
                TotalPotentialSavings = 187.50m,
                GeneratedAt = DateTime.UtcNow,
                Recommendations = new List<CostOptimizationRecommendation>
                {
                    new()
                    {
                        Id = Guid.NewGuid().ToString(),
                        Title = "Switch to more cost-effective models",
                        Description = "Consider using GPT-3.5-turbo instead of GPT-4 for routine tasks",
                        Category = "Model Selection",
                        PotentialSavings = 125.00m,
                        SavingsPercentage = 15.5,
                        ImplementationEffort = "low",
                        Priority = "high",
                        ImplementationSteps = new List<string>
                        {
                            "Identify routine tasks currently using GPT-4",
                            "Test GPT-3.5-turbo performance on sample tasks",
                            "Gradually migrate suitable workloads"
                        }
                    },
                    new()
                    {
                        Id = Guid.NewGuid().ToString(),
                        Title = "Implement request caching",
                        Description = "Cache similar requests to reduce API calls",
                        Category = "Caching",
                        PotentialSavings = 62.50m,
                        SavingsPercentage = 8.2,
                        ImplementationEffort = "medium",
                        Priority = "medium",
                        ImplementationSteps = new List<string>
                        {
                            "Implement Redis caching layer",
                            "Configure cache TTL policies",
                            "Monitor cache hit rates"
                        }
                    }
                },
                QuickWins = new List<CostOptimizationRecommendation>
                {
                    new()
                    {
                        Id = Guid.NewGuid().ToString(),
                        Title = "Optimize prompt length",
                        Description = "Reduce unnecessary tokens in prompts",
                        Category = "Prompt Optimization",
                        PotentialSavings = 25.00m,
                        SavingsPercentage = 3.3,
                        ImplementationEffort = "low",
                        Priority = "medium"
                    }
                }
            };

            return recommendations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cost optimization recommendations for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<CostForecast> GetCostForecastAsync(CostForecastRequest request, string userId)
    {
        try
        {
            _logger.LogInformation("Getting cost forecast for user {UserId}", userId);

            // This would typically use machine learning models for forecasting
            // For now, return a simulated forecast
            var forecast = new CostForecast
            {
                ForecastStart = request.ForecastStart,
                ForecastEnd = request.ForecastEnd,
                PredictedTotalCost = 1400.00m,
                ConfidenceLevel = 0.85,
                Methodology = request.ForecastModel,
                FactorsConsidered = new List<string>
                {
                    "Historical usage patterns",
                    "Seasonal trends",
                    "Model pricing changes",
                    "Usage growth rate"
                },
                DailyForecasts = new List<CostForecastPoint>()
            };

            // Generate daily forecast points
            var currentDate = request.ForecastStart;
            var dailyAverage = forecast.PredictedTotalCost / (decimal)(request.ForecastEnd - request.ForecastStart).TotalDays;

            while (currentDate <= request.ForecastEnd)
            {
                var variance = (decimal)(new Random().NextDouble() * 0.2 - 0.1); // ±10% variance
                var dailyCost = dailyAverage * (1 + variance);

                forecast.DailyForecasts.Add(new CostForecastPoint
                {
                    Date = currentDate,
                    PredictedCost = dailyCost,
                    LowerBound = dailyCost * 0.85m,
                    UpperBound = dailyCost * 1.15m,
                    Confidence = 0.85
                });

                currentDate = currentDate.AddDays(1);
            }

            return forecast;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cost forecast for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<decimal> GetTotalCostAsync(string userId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var (totalCost, _) = await _repository.GetTotalCostAsync(
                userId, startDate, endDate, null, null, null, null, null);
            return totalCost;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get total cost for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<CostAlert> CreateCostAlertAsync(CreateCostAlertRequest request, string userId)
    {
        try
        {
            _logger.LogInformation("Creating cost alert for user {UserId}", userId);

            var alert = new CostAlert
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Name = request.Name,
                Type = request.Type,
                ThresholdAmount = request.ThresholdAmount,
                ThresholdPercentage = request.ThresholdPercentage,
                TimePeriod = request.TimePeriod,
                IsActive = true,
                NotificationChannels = request.NotificationChannels,
                CreatedAt = DateTime.UtcNow,
                TriggerCount = 0
            };

            return await _repository.CreateCostAlertAsync(alert);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create cost alert for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CostAlert>> GetCostAlertsAsync(string userId, bool? isActive = null)
    {
        try
        {
            _logger.LogInformation("Getting cost alerts for user {UserId}", userId);
            return await _repository.GetCostAlertsAsync(userId, isActive?.ToString() ?? "all");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cost alerts for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<CostAlert> UpdateCostAlertAsync(string alertId, UpdateCostAlertRequest request, string userId)
    {
        try
        {
            _logger.LogInformation("Updating cost alert {AlertId} for user {UserId}", alertId, userId);

            var alert = await _repository.GetCostAlertAsync(alertId);
            if (alert == null || alert.UserId != userId)
            {
                throw new NotFoundException($"Cost alert {alertId} not found");
            }

            if (request.Name != null) alert.Name = request.Name;
            if (request.ThresholdAmount.HasValue) alert.ThresholdAmount = request.ThresholdAmount;
            if (request.ThresholdPercentage.HasValue) alert.ThresholdPercentage = request.ThresholdPercentage;
            if (request.IsActive.HasValue) alert.IsActive = request.IsActive.Value;
            if (request.NotificationChannels != null) alert.NotificationChannels = request.NotificationChannels;

            return await _repository.UpdateCostAlertAsync(alert);
        }
        catch (Exception ex) when (ex is not NotFoundException)
        {
            _logger.LogError(ex, "Failed to update cost alert {AlertId} for user {UserId}", alertId, userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteCostAlertAsync(string alertId, string userId)
    {
        try
        {
            _logger.LogInformation("Deleting cost alert {AlertId} for user {UserId}", alertId, userId);

            var alert = await _repository.GetCostAlertAsync(alertId);
            if (alert == null || alert.UserId != userId)
            {
                throw new NotFoundException($"Cost alert {alertId} not found");
            }

            await _repository.DeleteCostAlertAsync(alertId);
        }
        catch (Exception ex) when (ex is not NotFoundException)
        {
            _logger.LogError(ex, "Failed to delete cost alert {AlertId} for user {UserId}", alertId, userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<CostAnomalyDetectionResult> DetectCostAnomaliesAsync(CostAnomalyDetectionRequest request, string userId)
    {
        try
        {
            _logger.LogInformation("Detecting cost anomalies for user {UserId}", userId);

            // This would typically use machine learning algorithms for anomaly detection
            // For now, return simulated results
            var result = new CostAnomalyDetectionResult
            {
                Anomalies = new List<CostAnomaly>
                {
                    new()
                    {
                        Id = Guid.NewGuid().ToString(),
                        Date = DateTime.UtcNow.AddDays(-2),
                        ActualCost = 150.75m,
                        ExpectedCost = 45.20m,
                        AnomalyScore = 0.85,
                        Severity = "high",
                        Description = "Unusual spike in API usage detected",
                        PotentialCauses = new List<string>
                        {
                            "Increased batch processing",
                            "New application deployment",
                            "Possible API abuse"
                        }
                    }
                },
                Summary = new CostAnomalySummary
                {
                    TotalAnomalies = 1,
                    HighSeverityAnomalies = 1,
                    MediumSeverityAnomalies = 0,
                    LowSeverityAnomalies = 0,
                    TotalExcessCost = 105.55m,
                    AverageAnomalyScore = 0.85
                },
                Recommendations = new List<string>
                {
                    "Review recent API usage patterns",
                    "Check for unauthorized access",
                    "Consider implementing rate limiting"
                }
            };

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect cost anomalies for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<CostBreakdown> GetCostBreakdownAsync(CostBreakdownRequest request, string userId)
    {
        try
        {
            _logger.LogInformation("Getting cost breakdown for user {UserId}", userId);

            // This would typically query the cost database with grouping
            // For now, return simulated breakdown
            var breakdown = new CostBreakdown
            {
                TotalCost = 1250.75m,
                Currency = request.Currency,
                BreakdownByDimension = new Dictionary<string, List<CostBreakdownItem>>
                {
                    [request.GroupBy] = new List<CostBreakdownItem>
                    {
                        new() { DimensionValue = "OpenAI", Cost = 750.45m, PercentageOfTotal = 60.0 },
                        new() { DimensionValue = "Anthropic", Cost = 350.20m, PercentageOfTotal = 28.0 },
                        new() { DimensionValue = "Cohere", Cost = 150.10m, PercentageOfTotal = 12.0 }
                    }
                }
            };

            return breakdown;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cost breakdown for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<CostTrends> GetCostTrendsAsync(CostTrendsRequest request, string userId)
    {
        try
        {
            _logger.LogInformation("Getting cost trends for user {UserId}", userId);

            // This would typically analyze historical cost data
            // For now, return simulated trends
            var trends = new CostTrends
            {
                PeriodOverPeriodChange = 125.50m,
                PeriodOverPeriodPercentageChange = 11.2,
                TrendDirection = "increasing",
                AverageDailyCost = 41.69m,
                PeakCostDay = DateTime.UtcNow.AddDays(-5),
                PeakCostAmount = 89.45m,
                CostVolatility = 15.3
            };

            return trends;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cost trends for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ExportData> ExportCostDataAsync(ExportCostDataRequest request, string userId)
    {
        try
        {
            _logger.LogInformation("Exporting cost data for user {UserId}", userId);

            // This would typically generate and export the actual data
            // For now, return simulated export info
            var exportData = new ExportData
            {
                Id = Guid.NewGuid().ToString(),
                FileName = $"cost-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.{request.Format.ToString().ToLower()}",
                DownloadUrl = $"/api/exports/{Guid.NewGuid()}",
                FileSizeBytes = 1024 * 50, // 50KB
                Format = request.Format,
                GeneratedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                RecordCount = 1500,
                Status = "completed",
                ProgressPercentage = 100.0
            };

            return exportData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export cost data for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<CostEfficiencyMetrics> GetCostEfficiencyMetricsAsync(CostEfficiencyRequest request, string userId)
    {
        try
        {
            _logger.LogInformation("Getting cost efficiency metrics for user {UserId}", userId);

            // This would typically calculate efficiency metrics from actual data
            // For now, return simulated metrics
            var metrics = new CostEfficiencyMetrics
            {
                CostPerRequest = 0.125m,
                CostPerToken = 0.00002m,
                CostPerSuccessfulRequest = 0.127m,
                EfficiencyScore = 85.5,
                BenchmarkComparison = new EfficiencyBenchmark
                {
                    BenchmarkName = "Industry Average",
                    YourEfficiencyScore = 85.5,
                    BenchmarkEfficiencyScore = 75.0,
                    PerformanceVsBenchmark = 14.0,
                    RankingPercentile = 78.5
                },
                EfficiencyTrends = new List<EfficiencyTrendPoint>
                {
                    new() { Date = DateTime.UtcNow.AddDays(-30), EfficiencyScore = 82.1 },
                    new() { Date = DateTime.UtcNow.AddDays(-15), EfficiencyScore = 84.3 },
                    new() { Date = DateTime.UtcNow, EfficiencyScore = 85.5 }
                },
                OptimizationOpportunities = new List<string>
                {
                    "Implement request caching to reduce redundant API calls",
                    "Optimize prompt engineering to reduce token usage",
                    "Consider using more cost-effective models for routine tasks"
                }
            };

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cost efficiency metrics for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ProviderCostComparison> GetProviderCostComparisonAsync(ProviderCostComparisonRequest request, string userId)
    {
        try
        {
            _logger.LogInformation("Getting provider cost comparison for user {UserId}", userId);

            // This would typically analyze costs across providers
            // For now, return simulated comparison
            var comparison = new ProviderCostComparison
            {
                ComparisonPeriod = new DateRange
                {
                    StartDate = request.StartDate ?? DateTime.UtcNow.AddDays(-30),
                    EndDate = request.EndDate ?? DateTime.UtcNow
                },
                ProviderComparisons = new List<ProviderCostData>
                {
                    new()
                    {
                        ProviderName = "OpenAI",
                        TotalCost = 750.45m,
                        RequestCount = 5000,
                        TokenCount = 2500000,
                        AverageCostPerRequest = 0.15m,
                        AverageCostPerToken = 0.0003m,
                        MarketShare = 60.0
                    },
                    new()
                    {
                        ProviderName = "Anthropic",
                        TotalCost = 350.20m,
                        RequestCount = 2800,
                        TokenCount = 1400000,
                        AverageCostPerRequest = 0.125m,
                        AverageCostPerToken = 0.00025m,
                        MarketShare = 28.0
                    },
                    new()
                    {
                        ProviderName = "Cohere",
                        TotalCost = 150.10m,
                        RequestCount = 1500,
                        TokenCount = 750000,
                        AverageCostPerRequest = 0.10m,
                        AverageCostPerToken = 0.0002m,
                        MarketShare = 12.0
                    }
                },
                CostSavingsOpportunities = new List<CostSavingsOpportunity>
                {
                    new()
                    {
                        Description = "Switch routine tasks from OpenAI to Cohere",
                        PotentialSavings = 125.00m,
                        SavingsPercentage = 10.0,
                        RiskLevel = "low",
                        ImplementationEffort = "medium"
                    }
                },
                Recommendations = new List<string>
                {
                    "Consider diversifying across providers to optimize costs",
                    "Test Cohere for routine tasks to reduce overall costs",
                    "Monitor provider pricing changes regularly"
                }
            };

            return comparison;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get provider cost comparison for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<CostAllocation> GetCostAllocationAsync(CostAllocationRequest request, string userId)
    {
        try
        {
            _logger.LogInformation("Getting cost allocation for user {UserId}", userId);

            // This would typically calculate cost allocation based on actual usage
            // For now, return simulated allocation
            var allocation = new CostAllocation
            {
                TotalCost = 1250.75m,
                Currency = request.Currency,
                AllocationMethod = request.AllocationMethod,
                Allocations = new List<CostAllocationItem>
                {
                    new()
                    {
                        EntityId = "team-frontend",
                        EntityName = "Frontend Team",
                        EntityType = "team",
                        AllocatedCost = 625.38m,
                        AllocationPercentage = 50.0,
                        AllocationBasis = "Usage-based allocation"
                    },
                    new()
                    {
                        EntityId = "team-backend",
                        EntityName = "Backend Team",
                        EntityType = "team",
                        AllocatedCost = 375.23m,
                        AllocationPercentage = 30.0,
                        AllocationBasis = "Usage-based allocation"
                    },
                    new()
                    {
                        EntityId = "team-ml",
                        EntityName = "ML Team",
                        EntityType = "team",
                        AllocatedCost = 250.14m,
                        AllocationPercentage = 20.0,
                        AllocationBasis = "Usage-based allocation"
                    }
                },
                AllocationRules = new List<string>
                {
                    "Allocate based on API usage volume"
                }
            };

            return allocation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cost allocation for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<CostCenter> CreateCostCenterAsync(CreateCostCenterRequest request, string userId)
    {
        try
        {
            _logger.LogInformation("Creating cost center for user {UserId}", userId);

            var costCenter = new CostCenter
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                Description = request.Description,
                ManagerUserId = request.ManagerUserId,
                BudgetLimit = request.BudgetLimit,
                Currency = request.Currency,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Tags = request.Tags ?? new List<string>(),
                AllocationRules = request.AllocationRules ?? new List<AllocationRule>()
            };

            return await _repository.CreateCostCenterAsync(costCenter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create cost center for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CostCenter>> GetCostCentersAsync(string userId)
    {
        try
        {
            _logger.LogInformation("Getting cost centers for user {UserId}", userId);
            return await _repository.GetCostCentersAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cost centers for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<RealTimeCostData> GetRealTimeCostDataAsync(string userId)
    {
        try
        {
            _logger.LogInformation("Getting real-time cost data for user {UserId}", userId);

            // This would typically query real-time cost tracking systems
            // For now, return simulated real-time data
            var realTimeData = new RealTimeCostData
            {
                CurrentCost = 45.67m,
                Currency = "USD",
                LastUpdated = DateTime.UtcNow,
                TodaysCost = 45.67m,
                YesterdaysCost = 42.15m,
                MonthToDateCost = 1250.75m,
                DailyBudget = 50.00m,
                MonthlyBudget = 1500.00m,
                BudgetUtilization = 83.4,
                CostVelocity = 1.85m, // Cost per hour
                ProjectedDailyCost = 48.20m,
                ProjectedMonthlyCost = 1446.00m,
                TopCostDrivers = new List<CostDriver>
                {
                    new() { Name = "GPT-4 API", Cost = 25.30m, Percentage = 55.4 },
                    new() { Name = "GPT-3.5 API", Cost = 12.15m, Percentage = 26.6 },
                    new() { Name = "Embeddings", Cost = 8.22m, Percentage = 18.0 }
                },
                RecentTransactions = new List<RecentTransaction>
                {
                    new()
                    {
                        Timestamp = DateTime.UtcNow.AddMinutes(-5),
                        Description = "GPT-4 completion request",
                        Cost = 0.15m,
                        Provider = "OpenAI",
                        ModelId = "gpt-4"
                    },
                    new()
                    {
                        Timestamp = DateTime.UtcNow.AddMinutes(-8),
                        Description = "Text embedding request",
                        Cost = 0.02m,
                        Provider = "OpenAI",
                        ModelId = "text-embedding-ada-002"
                    }
                }
            };

            return realTimeData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get real-time cost data for user {UserId}", userId);
            throw;
        }
    }

    #endregion
}
