namespace LLMGateway.Core.Models.TokenUsage;

/// <summary>
/// Summary of token usage
/// </summary>
public class TokenUsageSummary
{
    /// <summary>
    /// Start date for the summary
    /// </summary>
    public DateTimeOffset StartDate { get; set; }

    /// <summary>
    /// End date for the summary
    /// </summary>
    public DateTimeOffset EndDate { get; set; }

    /// <summary>
    /// Total prompt tokens
    /// </summary>
    public int TotalPromptTokens { get; set; }

    /// <summary>
    /// Total completion tokens
    /// </summary>
    public int TotalCompletionTokens { get; set; }

    /// <summary>
    /// Total tokens
    /// </summary>
    public int TotalTokens { get; set; }

    /// <summary>
    /// Total estimated cost in USD
    /// </summary>
    public decimal TotalEstimatedCostUsd { get; set; }

    /// <summary>
    /// Total request count
    /// </summary>
    public int RequestCount { get; set; }

    /// <summary>
    /// Number of unique users
    /// </summary>
    public int UniqueUsers { get; set; }

    /// <summary>
    /// Number of unique models
    /// </summary>
    public int UniqueModels { get; set; }

    /// <summary>
    /// Top models by usage
    /// </summary>
    public List<ModelUsage> TopModels { get; set; } = new();

    /// <summary>
    /// Top users by usage
    /// </summary>
    public List<UserUsage> TopUsers { get; set; } = new();

    /// <summary>
    /// Provider usage
    /// </summary>
    public List<ProviderUsage> ProviderUsage { get; set; } = new();

    /// <summary>
    /// Usage by model
    /// </summary>
    public Dictionary<string, ModelUsage> UsageByModel { get; set; } = new();

    /// <summary>
    /// Usage by provider
    /// </summary>
    public Dictionary<string, ProviderUsage> UsageByProvider { get; set; } = new();

    /// <summary>
    /// Usage by user
    /// </summary>
    public Dictionary<string, UserUsage> UsageByUser { get; set; } = new();
}

/// <summary>
/// Usage for a model
/// </summary>
public class ModelUsage
{
    /// <summary>
    /// Model ID
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Provider name
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Prompt tokens
    /// </summary>
    public int PromptTokens { get; set; }

    /// <summary>
    /// Completion tokens
    /// </summary>
    public int CompletionTokens { get; set; }

    /// <summary>
    /// Total tokens
    /// </summary>
    public int TotalTokens { get; set; }

    /// <summary>
    /// Estimated cost in USD
    /// </summary>
    public decimal EstimatedCostUsd { get; set; }

    /// <summary>
    /// Request count
    /// </summary>
    public int RequestCount { get; set; }
}

/// <summary>
/// Usage for a provider
/// </summary>
public class ProviderUsage
{
    /// <summary>
    /// Provider name
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Prompt tokens
    /// </summary>
    public int PromptTokens { get; set; }

    /// <summary>
    /// Completion tokens
    /// </summary>
    public int CompletionTokens { get; set; }

    /// <summary>
    /// Total tokens
    /// </summary>
    public int TotalTokens { get; set; }

    /// <summary>
    /// Estimated cost in USD
    /// </summary>
    public decimal EstimatedCostUsd { get; set; }

    /// <summary>
    /// Request count
    /// </summary>
    public int RequestCount { get; set; }
}

/// <summary>
/// Usage for a user
/// </summary>
public class UserUsage
{
    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Prompt tokens
    /// </summary>
    public int PromptTokens { get; set; }

    /// <summary>
    /// Completion tokens
    /// </summary>
    public int CompletionTokens { get; set; }

    /// <summary>
    /// Total tokens
    /// </summary>
    public int TotalTokens { get; set; }

    /// <summary>
    /// Estimated cost in USD
    /// </summary>
    public decimal EstimatedCostUsd { get; set; }

    /// <summary>
    /// Request count
    /// </summary>
    public int RequestCount { get; set; }
}
