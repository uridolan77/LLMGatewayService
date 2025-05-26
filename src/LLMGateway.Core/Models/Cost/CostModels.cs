namespace LLMGateway.Core.Models.Cost;

/// <summary>
/// Cost record
/// </summary>
public class CostRecord
{
    /// <summary>
    /// Record ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Request ID
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Provider
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Model ID
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Operation type
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Input tokens
    /// </summary>
    public int InputTokens { get; set; }

    /// <summary>
    /// Output tokens
    /// </summary>
    public int OutputTokens { get; set; }

    /// <summary>
    /// Total tokens
    /// </summary>
    public int TotalTokens { get; set; }

    /// <summary>
    /// Cost in USD
    /// </summary>
    public decimal CostUsd { get; set; }

    /// <summary>
    /// Project ID
    /// </summary>
    public string? ProjectId { get; set; }

    /// <summary>
    /// Tags
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Budget
/// </summary>
public class Budget
{
    /// <summary>
    /// Budget ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Budget name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Budget description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Project ID
    /// </summary>
    public string? ProjectId { get; set; }

    /// <summary>
    /// Amount in USD
    /// </summary>
    public decimal AmountUsd { get; set; }

    /// <summary>
    /// Start date
    /// </summary>
    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// End date
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Reset period
    /// </summary>
    public BudgetResetPeriod ResetPeriod { get; set; } = BudgetResetPeriod.Never;

    /// <summary>
    /// Alert threshold percentage
    /// </summary>
    public int AlertThresholdPercentage { get; set; } = 80;

    /// <summary>
    /// Whether to enforce the budget
    /// </summary>
    public bool EnforceBudget { get; set; } = false;

    /// <summary>
    /// Created at
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Updated at
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Tags
    /// </summary>
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Budget reset period
/// </summary>
public enum BudgetResetPeriod
{
    /// <summary>
    /// Never reset
    /// </summary>
    Never,

    /// <summary>
    /// Reset daily
    /// </summary>
    Daily,

    /// <summary>
    /// Reset weekly
    /// </summary>
    Weekly,

    /// <summary>
    /// Reset monthly
    /// </summary>
    Monthly,

    /// <summary>
    /// Reset quarterly
    /// </summary>
    Quarterly,

    /// <summary>
    /// Reset yearly
    /// </summary>
    Yearly
}

/// <summary>
/// Cost report
/// </summary>
public class CostReport
{
    /// <summary>
    /// Start date
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Grouping
    /// </summary>
    public string Grouping { get; set; } = string.Empty;

    /// <summary>
    /// Total cost in USD
    /// </summary>
    public decimal TotalCostUsd { get; set; }

    /// <summary>
    /// Total tokens
    /// </summary>
    public int TotalTokens { get; set; }

    /// <summary>
    /// Cost breakdown
    /// </summary>
    public List<CostBreakdown> Breakdown { get; set; } = new();
}



/// <summary>
/// Cost report request
/// </summary>
public class CostReportRequest
{
    /// <summary>
    /// Start date
    /// </summary>
    public DateTime StartDate { get; set; } = DateTime.UtcNow.AddDays(-30);

    /// <summary>
    /// End date
    /// </summary>
    public DateTime EndDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Group by
    /// </summary>
    public string GroupBy { get; set; } = "provider";

    /// <summary>
    /// Filter by provider
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// Filter by model ID
    /// </summary>
    public string? ModelId { get; set; }

    /// <summary>
    /// Filter by operation type
    /// </summary>
    public string? OperationType { get; set; }

    /// <summary>
    /// Filter by project ID
    /// </summary>
    public string? ProjectId { get; set; }

    /// <summary>
    /// Filter by tags
    /// </summary>
    public List<string>? Tags { get; set; }
}

/// <summary>
/// Create budget request
/// </summary>
public class CreateBudgetRequest
{
    /// <summary>
    /// Budget name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Budget description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Project ID
    /// </summary>
    public string? ProjectId { get; set; }

    /// <summary>
    /// Amount in USD
    /// </summary>
    public decimal AmountUsd { get; set; }

    /// <summary>
    /// Start date
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Reset period
    /// </summary>
    public BudgetResetPeriod ResetPeriod { get; set; } = BudgetResetPeriod.Never;

    /// <summary>
    /// Alert threshold percentage
    /// </summary>
    public int AlertThresholdPercentage { get; set; } = 80;

    /// <summary>
    /// Whether to enforce the budget
    /// </summary>
    public bool EnforceBudget { get; set; } = false;

    /// <summary>
    /// Tags
    /// </summary>
    public List<string>? Tags { get; set; }
}

/// <summary>
/// Update budget request
/// </summary>
public class UpdateBudgetRequest
{
    /// <summary>
    /// Budget name
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Budget description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Project ID
    /// </summary>
    public string? ProjectId { get; set; }

    /// <summary>
    /// Amount in USD
    /// </summary>
    public decimal? AmountUsd { get; set; }

    /// <summary>
    /// End date
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Reset period
    /// </summary>
    public BudgetResetPeriod? ResetPeriod { get; set; }

    /// <summary>
    /// Alert threshold percentage
    /// </summary>
    public int? AlertThresholdPercentage { get; set; }

    /// <summary>
    /// Whether to enforce the budget
    /// </summary>
    public bool? EnforceBudget { get; set; }

    /// <summary>
    /// Tags
    /// </summary>
    public List<string>? Tags { get; set; }
}

/// <summary>
/// Budget usage
/// </summary>
public class BudgetUsage
{
    /// <summary>
    /// Budget ID
    /// </summary>
    public string BudgetId { get; set; } = string.Empty;

    /// <summary>
    /// Budget name
    /// </summary>
    public string BudgetName { get; set; } = string.Empty;

    /// <summary>
    /// Amount in USD
    /// </summary>
    public decimal AmountUsd { get; set; }

    /// <summary>
    /// Used amount in USD
    /// </summary>
    public decimal UsedAmountUsd { get; set; }

    /// <summary>
    /// Remaining amount in USD
    /// </summary>
    public decimal RemainingAmountUsd { get; set; }

    /// <summary>
    /// Usage percentage
    /// </summary>
    public decimal UsagePercentage { get; set; }

    /// <summary>
    /// Start date
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Reset period
    /// </summary>
    public BudgetResetPeriod ResetPeriod { get; set; }

    /// <summary>
    /// Next reset date
    /// </summary>
    public DateTime? NextResetDate { get; set; }

    /// <summary>
    /// Alert threshold percentage
    /// </summary>
    public int AlertThresholdPercentage { get; set; }

    /// <summary>
    /// Whether the budget is enforced
    /// </summary>
    public bool EnforceBudget { get; set; }

    /// <summary>
    /// Whether the budget is exceeded
    /// </summary>
    public bool IsBudgetExceeded { get; set; }

    /// <summary>
    /// Whether the alert threshold is reached
    /// </summary>
    public bool IsAlertThresholdReached { get; set; }
}
