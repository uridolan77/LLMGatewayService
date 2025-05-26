using System.Text.Json.Serialization;

namespace LLMGateway.Core.Models.FineTuning;

/// <summary>
/// Fine-tuning job status
/// </summary>
public enum FineTuningJobStatus
{
    /// <summary>
    /// Job is created but not yet started
    /// </summary>
    Created,

    /// <summary>
    /// Job is waiting in the queue
    /// </summary>
    Queued,

    /// <summary>
    /// Job is running
    /// </summary>
    Running,

    /// <summary>
    /// Job is completed successfully
    /// </summary>
    Succeeded,

    /// <summary>
    /// Job failed
    /// </summary>
    Failed,

    /// <summary>
    /// Job was cancelled
    /// </summary>
    Cancelled
}

/// <summary>
/// Fine-tuning job
/// </summary>
public class FineTuningJob
{
    /// <summary>
    /// Job ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Job name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Job description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Provider (e.g., OpenAI, Azure OpenAI)
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Base model ID
    /// </summary>
    public string BaseModelId { get; set; } = string.Empty;

    /// <summary>
    /// Fine-tuned model ID (after completion)
    /// </summary>
    public string? FineTunedModelId { get; set; }

    /// <summary>
    /// Training file ID
    /// </summary>
    public string TrainingFileId { get; set; } = string.Empty;

    /// <summary>
    /// Validation file ID
    /// </summary>
    public string? ValidationFileId { get; set; }

    /// <summary>
    /// Hyperparameters
    /// </summary>
    public FineTuningHyperparameters Hyperparameters { get; set; } = new();

    /// <summary>
    /// Status
    /// </summary>
    public FineTuningJobStatus Status { get; set; } = FineTuningJobStatus.Created;

    /// <summary>
    /// Created by
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Created at
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Started at
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Completed at
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Error message (if failed)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Training metrics
    /// </summary>
    public FineTuningMetrics? Metrics { get; set; }

    /// <summary>
    /// Provider-specific job ID
    /// </summary>
    public string? ProviderJobId { get; set; }

    /// <summary>
    /// Tags
    /// </summary>
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Fine-tuning hyperparameters
/// </summary>
public class FineTuningHyperparameters
{
    /// <summary>
    /// Number of epochs
    /// </summary>
    public int? Epochs { get; set; }

    /// <summary>
    /// Batch size
    /// </summary>
    public int? BatchSize { get; set; }

    /// <summary>
    /// Learning rate
    /// </summary>
    public float? LearningRate { get; set; }

    /// <summary>
    /// Learning rate multiplier
    /// </summary>
    public float? LearningRateMultiplier { get; set; }

    /// <summary>
    /// Prompt loss weight
    /// </summary>
    public float? PromptLossWeight { get; set; }

    /// <summary>
    /// Compute classification metrics
    /// </summary>
    public bool? ComputeClassificationMetrics { get; set; }

    /// <summary>
    /// Classification n classes
    /// </summary>
    public int? ClassificationNClasses { get; set; }

    /// <summary>
    /// Classification positive class
    /// </summary>
    public string? ClassificationPositiveClass { get; set; }

    /// <summary>
    /// Suffix
    /// </summary>
    public string? Suffix { get; set; }
}

/// <summary>
/// Fine-tuning metrics
/// </summary>
public class FineTuningMetrics
{
    /// <summary>
    /// Training loss
    /// </summary>
    public float? TrainingLoss { get; set; }

    /// <summary>
    /// Validation loss
    /// </summary>
    public float? ValidationLoss { get; set; }

    /// <summary>
    /// Training accuracy
    /// </summary>
    public float? TrainingAccuracy { get; set; }

    /// <summary>
    /// Validation accuracy
    /// </summary>
    public float? ValidationAccuracy { get; set; }

    /// <summary>
    /// Training samples
    /// </summary>
    public int? TrainingSamples { get; set; }

    /// <summary>
    /// Validation samples
    /// </summary>
    public int? ValidationSamples { get; set; }

    /// <summary>
    /// Elapsed tokens
    /// </summary>
    public int? ElapsedTokens { get; set; }

    /// <summary>
    /// Elapsed examples
    /// </summary>
    public int? ElapsedExamples { get; set; }

    /// <summary>
    /// Training step metrics
    /// </summary>
    public List<FineTuningStepMetric> TrainingSteps { get; set; } = new();
}

/// <summary>
/// Fine-tuning step metric
/// </summary>
public class FineTuningStepMetric
{
    /// <summary>
    /// Step
    /// </summary>
    public int Step { get; set; }

    /// <summary>
    /// Timestamp
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Loss
    /// </summary>
    public float Loss { get; set; }

    /// <summary>
    /// Accuracy
    /// </summary>
    public float? Accuracy { get; set; }

    /// <summary>
    /// Elapsed tokens
    /// </summary>
    public int? ElapsedTokens { get; set; }
}

/// <summary>
/// Fine-tuning file
/// </summary>
public class FineTuningFile
{
    /// <summary>
    /// File ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// File name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// File purpose
    /// </summary>
    public string Purpose { get; set; } = "fine-tune";

    /// <summary>
    /// Created by
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Created at
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Provider (e.g., OpenAI, Azure OpenAI)
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Provider-specific file ID
    /// </summary>
    public string? ProviderFileId { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public string Status { get; set; } = "uploaded";
}

/// <summary>
/// Create fine-tuning job request
/// </summary>
public class CreateFineTuningJobRequest
{
    /// <summary>
    /// Job name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Job description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Provider (e.g., OpenAI, Azure OpenAI)
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Base model ID
    /// </summary>
    public string BaseModelId { get; set; } = string.Empty;

    /// <summary>
    /// Training file ID
    /// </summary>
    public string TrainingFileId { get; set; } = string.Empty;

    /// <summary>
    /// Validation file ID
    /// </summary>
    public string? ValidationFileId { get; set; }

    /// <summary>
    /// Hyperparameters
    /// </summary>
    public FineTuningHyperparameters? Hyperparameters { get; set; }

    /// <summary>
    /// Tags
    /// </summary>
    public List<string>? Tags { get; set; }
}

/// <summary>
/// Upload fine-tuning file request
/// </summary>
public class UploadFineTuningFileRequest
{
    /// <summary>
    /// File name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// File purpose
    /// </summary>
    public string Purpose { get; set; } = "fine-tune";

    /// <summary>
    /// Provider (e.g., OpenAI, Azure OpenAI)
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// File content (base64 encoded)
    /// </summary>
    public string FileContent { get; set; } = string.Empty;
}

/// <summary>
/// Fine-tuning job search request
/// </summary>
public class FineTuningJobSearchRequest
{
    /// <summary>
    /// Search query
    /// </summary>
    public string? Query { get; set; }

    /// <summary>
    /// Filter by status
    /// </summary>
    public FineTuningJobStatus? Status { get; set; }

    /// <summary>
    /// Filter by provider
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// Filter by base model ID
    /// </summary>
    public string? BaseModelId { get; set; }

    /// <summary>
    /// Filter by tags
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Filter by created by
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Page number
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Fine-tuning job search response
/// </summary>
public class FineTuningJobSearchResponse
{
    /// <summary>
    /// Jobs
    /// </summary>
    public List<FineTuningJob> Jobs { get; set; } = new();

    /// <summary>
    /// Total count
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Page number
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total pages
    /// </summary>
    public int TotalPages { get; set; }
}

/// <summary>
/// Fine-tuning analytics
/// </summary>
public class FineTuningAnalytics
{
    /// <summary>
    /// Total fine-tuning jobs
    /// </summary>
    public int TotalJobs { get; set; }

    /// <summary>
    /// Successful jobs
    /// </summary>
    public int SuccessfulJobs { get; set; }

    /// <summary>
    /// Failed jobs
    /// </summary>
    public int FailedJobs { get; set; }

    /// <summary>
    /// Cancelled jobs
    /// </summary>
    public int CancelledJobs { get; set; }

    /// <summary>
    /// Running jobs
    /// </summary>
    public int RunningJobs { get; set; }

    /// <summary>
    /// Success rate
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Average training time
    /// </summary>
    public TimeSpan AverageTrainingTime { get; set; }

    /// <summary>
    /// Total training time
    /// </summary>
    public TimeSpan TotalTrainingTime { get; set; }

    /// <summary>
    /// Total cost
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Average cost per job
    /// </summary>
    public decimal AverageCostPerJob { get; set; }

    /// <summary>
    /// Popular base models
    /// </summary>
    public List<ModelUsageStats> PopularBaseModels { get; set; } = new();

    /// <summary>
    /// Performance metrics
    /// </summary>
    public FineTuningPerformanceMetrics? PerformanceMetrics { get; set; }

    /// <summary>
    /// Jobs by status
    /// </summary>
    public Dictionary<string, int> JobsByStatus { get; set; } = new();

    /// <summary>
    /// Jobs by model
    /// </summary>
    public Dictionary<string, int> JobsByModel { get; set; } = new();

    /// <summary>
    /// Jobs by provider
    /// </summary>
    public Dictionary<string, int> JobsByProvider { get; set; } = new();

    /// <summary>
    /// Training time trends
    /// </summary>
    public List<TrainingTimeTrend> TrainingTimeTrends { get; set; } = new();

    /// <summary>
    /// Cost trends
    /// </summary>
    public List<CostTrend> CostTrends { get; set; } = new();

    /// <summary>
    /// Performance improvements
    /// </summary>
    public List<PerformanceImprovement> PerformanceImprovements { get; set; } = new();
}

/// <summary>
/// Training time trend
/// </summary>
public class TrainingTimeTrend
{
    /// <summary>
    /// Date
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Average training time
    /// </summary>
    public TimeSpan AverageTrainingTime { get; set; }

    /// <summary>
    /// Job count
    /// </summary>
    public int JobCount { get; set; }
}

/// <summary>
/// Cost trend
/// </summary>
public class CostTrend
{
    /// <summary>
    /// Date
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Total cost
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Average cost per job
    /// </summary>
    public decimal AverageCostPerJob { get; set; }

    /// <summary>
    /// Job count
    /// </summary>
    public int JobCount { get; set; }
}

/// <summary>
/// Performance improvement
/// </summary>
public class PerformanceImprovement
{
    /// <summary>
    /// Model ID
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Base model performance
    /// </summary>
    public double BaseModelPerformance { get; set; }

    /// <summary>
    /// Fine-tuned model performance
    /// </summary>
    public double FineTunedModelPerformance { get; set; }

    /// <summary>
    /// Improvement percentage
    /// </summary>
    public double ImprovementPercentage { get; set; }

    /// <summary>
    /// Metric name
    /// </summary>
    public string MetricName { get; set; } = string.Empty;
}

/// <summary>
/// Fine-tuning cost breakdown
/// </summary>
public class FineTuningCostBreakdown
{
    /// <summary>
    /// Total cost
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Currency
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Training cost
    /// </summary>
    public decimal TrainingCost { get; set; }

    /// <summary>
    /// Data preparation cost
    /// </summary>
    public decimal DataPreparationCost { get; set; }

    /// <summary>
    /// Validation cost
    /// </summary>
    public decimal ValidationCost { get; set; }

    /// <summary>
    /// Storage cost
    /// </summary>
    public decimal StorageCost { get; set; }

    /// <summary>
    /// Compute cost
    /// </summary>
    public decimal ComputeCost { get; set; }

    /// <summary>
    /// Cost by provider
    /// </summary>
    public Dictionary<string, decimal> CostByProvider { get; set; } = new();

    /// <summary>
    /// Breakdown by provider
    /// </summary>
    public Dictionary<string, decimal> BreakdownByProvider { get; set; } = new();

    /// <summary>
    /// Cost by model
    /// </summary>
    public Dictionary<string, decimal> CostByModel { get; set; } = new();

    /// <summary>
    /// Breakdown by model
    /// </summary>
    public Dictionary<string, decimal> BreakdownByModel { get; set; } = new();

    /// <summary>
    /// Cost by job status
    /// </summary>
    public Dictionary<string, decimal> CostByJobStatus { get; set; } = new();

    /// <summary>
    /// Breakdown by job type
    /// </summary>
    public Dictionary<string, decimal> BreakdownByJobType { get; set; } = new();

    /// <summary>
    /// Training costs
    /// </summary>
    public decimal TrainingCosts { get; set; }

    /// <summary>
    /// Validation costs
    /// </summary>
    public decimal ValidationCosts { get; set; }

    /// <summary>
    /// Storage costs
    /// </summary>
    public decimal StorageCosts { get; set; }

    /// <summary>
    /// Cost trends
    /// </summary>
    public List<CostTrendPoint> CostTrends { get; set; } = new();

    /// <summary>
    /// Cost breakdown over time
    /// </summary>
    public List<FineTuningCostBreakdownTimePoint> CostBreakdownOverTime { get; set; } = new();

    /// <summary>
    /// Cost optimization opportunities
    /// </summary>
    public List<CostOptimizationOpportunity> OptimizationOpportunities { get; set; } = new();
}

/// <summary>
/// Fine-tuning cost breakdown time point
/// </summary>
public class FineTuningCostBreakdownTimePoint
{
    /// <summary>
    /// Date
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Training cost
    /// </summary>
    public decimal TrainingCost { get; set; }

    /// <summary>
    /// Data preparation cost
    /// </summary>
    public decimal DataPreparationCost { get; set; }

    /// <summary>
    /// Validation cost
    /// </summary>
    public decimal ValidationCost { get; set; }

    /// <summary>
    /// Storage cost
    /// </summary>
    public decimal StorageCost { get; set; }

    /// <summary>
    /// Total cost
    /// </summary>
    public decimal TotalCost { get; set; }
}

/// <summary>
/// Cost optimization opportunity
/// </summary>
public class CostOptimizationOpportunity
{
    /// <summary>
    /// Opportunity type
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Potential savings
    /// </summary>
    public decimal PotentialSavings { get; set; }

    /// <summary>
    /// Savings percentage
    /// </summary>
    public double SavingsPercentage { get; set; }

    /// <summary>
    /// Implementation effort
    /// </summary>
    public string ImplementationEffort { get; set; } = string.Empty;

    /// <summary>
    /// Priority
    /// </summary>
    public string Priority { get; set; } = string.Empty;

    /// <summary>
    /// Affected jobs
    /// </summary>
    public List<string> AffectedJobs { get; set; } = new();
}

/// <summary>
/// Fine-tuning cost estimate request
/// </summary>
public class EstimateFineTuningCostRequest
{
    /// <summary>
    /// Base model ID
    /// </summary>
    public string BaseModelId { get; set; } = string.Empty;

    /// <summary>
    /// Provider
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Training data size (number of examples)
    /// </summary>
    public int TrainingDataSize { get; set; }

    /// <summary>
    /// Estimated training epochs
    /// </summary>
    public int EstimatedEpochs { get; set; } = 3;

    /// <summary>
    /// Training data complexity (simple, medium, complex)
    /// </summary>
    public string DataComplexity { get; set; } = "medium";

    /// <summary>
    /// Include validation data
    /// </summary>
    public bool IncludeValidation { get; set; } = true;

    /// <summary>
    /// Currency
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Additional parameters
    /// </summary>
    public Dictionary<string, object> AdditionalParameters { get; set; } = new();
}

/// <summary>
/// Fine-tuning cost estimate
/// </summary>
public class FineTuningCostEstimate
{
    /// <summary>
    /// Estimated total cost
    /// </summary>
    public decimal EstimatedTotalCost { get; set; }

    /// <summary>
    /// Currency
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Training cost
    /// </summary>
    public decimal TrainingCost { get; set; }

    /// <summary>
    /// Data preparation cost
    /// </summary>
    public decimal DataPreparationCost { get; set; }

    /// <summary>
    /// Validation cost
    /// </summary>
    public decimal ValidationCost { get; set; }

    /// <summary>
    /// Storage cost
    /// </summary>
    public decimal StorageCost { get; set; }

    /// <summary>
    /// Cost breakdown by component
    /// </summary>
    public Dictionary<string, decimal> CostBreakdown { get; set; } = new();

    /// <summary>
    /// Estimated training time
    /// </summary>
    public TimeSpan EstimatedTrainingTime { get; set; }

    /// <summary>
    /// Confidence level (0-1)
    /// </summary>
    public double ConfidenceLevel { get; set; }

    /// <summary>
    /// Cost factors considered
    /// </summary>
    public List<string> CostFactors { get; set; } = new();

    /// <summary>
    /// Optimization suggestions
    /// </summary>
    public List<string> OptimizationSuggestions { get; set; } = new();

    /// <summary>
    /// Generated at
    /// </summary>
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Model performance comparison
/// </summary>
public class ModelPerformanceComparison
{
    /// <summary>
    /// Base model ID
    /// </summary>
    public string BaseModelId { get; set; } = string.Empty;

    /// <summary>
    /// Fine-tuned model ID
    /// </summary>
    public string FineTunedModelId { get; set; } = string.Empty;

    /// <summary>
    /// Comparison metrics
    /// </summary>
    public Dictionary<string, PerformanceMetric> Metrics { get; set; } = new();

    /// <summary>
    /// Overall improvement score
    /// </summary>
    public double OverallImprovementScore { get; set; }

    /// <summary>
    /// Performance summary
    /// </summary>
    public PerformanceComparisonSummary Summary { get; set; } = new();

    /// <summary>
    /// Detailed analysis
    /// </summary>
    public List<PerformanceAnalysisItem> DetailedAnalysis { get; set; } = new();

    /// <summary>
    /// Recommendations
    /// </summary>
    public List<string> Recommendations { get; set; } = new();

    /// <summary>
    /// Test results
    /// </summary>
    public List<TestResult> TestResults { get; set; } = new();

    /// <summary>
    /// Generated at
    /// </summary>
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Performance metric
/// </summary>
public class PerformanceMetric
{
    /// <summary>
    /// Metric name
    /// </summary>
    public string MetricName { get; set; } = string.Empty;

    /// <summary>
    /// Base model value
    /// </summary>
    public double BaseModelValue { get; set; }

    /// <summary>
    /// Fine-tuned model value
    /// </summary>
    public double FineTunedModelValue { get; set; }

    /// <summary>
    /// Improvement percentage
    /// </summary>
    public double ImprovementPercentage { get; set; }

    /// <summary>
    /// Is improvement
    /// </summary>
    public bool IsImprovement { get; set; }

    /// <summary>
    /// Significance level
    /// </summary>
    public double SignificanceLevel { get; set; }

    /// <summary>
    /// Unit
    /// </summary>
    public string Unit { get; set; } = string.Empty;
}

/// <summary>
/// Performance comparison summary
/// </summary>
public class PerformanceComparisonSummary
{
    /// <summary>
    /// Total metrics compared
    /// </summary>
    public int TotalMetricsCompared { get; set; }

    /// <summary>
    /// Metrics improved
    /// </summary>
    public int MetricsImproved { get; set; }

    /// <summary>
    /// Metrics degraded
    /// </summary>
    public int MetricsDegraded { get; set; }

    /// <summary>
    /// Metrics unchanged
    /// </summary>
    public int MetricsUnchanged { get; set; }

    /// <summary>
    /// Average improvement
    /// </summary>
    public double AverageImprovement { get; set; }

    /// <summary>
    /// Best performing metric
    /// </summary>
    public string? BestPerformingMetric { get; set; }

    /// <summary>
    /// Worst performing metric
    /// </summary>
    public string? WorstPerformingMetric { get; set; }
}

/// <summary>
/// Performance analysis item
/// </summary>
public class PerformanceAnalysisItem
{
    /// <summary>
    /// Category
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Finding
    /// </summary>
    public string Finding { get; set; } = string.Empty;

    /// <summary>
    /// Impact level
    /// </summary>
    public string ImpactLevel { get; set; } = string.Empty;

    /// <summary>
    /// Supporting data
    /// </summary>
    public Dictionary<string, object> SupportingData { get; set; } = new();
}

/// <summary>
/// Test result
/// </summary>
public class TestResult
{
    /// <summary>
    /// Test name
    /// </summary>
    public string TestName { get; set; } = string.Empty;

    /// <summary>
    /// Base model score
    /// </summary>
    public double BaseModelScore { get; set; }

    /// <summary>
    /// Fine-tuned model score
    /// </summary>
    public double FineTunedModelScore { get; set; }

    /// <summary>
    /// Test type
    /// </summary>
    public string TestType { get; set; } = string.Empty;

    /// <summary>
    /// Dataset used
    /// </summary>
    public string DatasetUsed { get; set; } = string.Empty;

    /// <summary>
    /// Sample size
    /// </summary>
    public int SampleSize { get; set; }
}

/// <summary>
/// Fine-tuning recommendations
/// </summary>
public class FineTuningRecommendations
{
    /// <summary>
    /// Model recommendations
    /// </summary>
    public List<ModelRecommendation> ModelRecommendations { get; set; } = new();

    /// <summary>
    /// Data recommendations
    /// </summary>
    public List<DataRecommendation> DataRecommendations { get; set; } = new();

    /// <summary>
    /// Training recommendations
    /// </summary>
    public List<TrainingRecommendation> TrainingRecommendations { get; set; } = new();

    /// <summary>
    /// Cost optimization recommendations
    /// </summary>
    public List<CostOptimizationRecommendation> CostOptimizationRecommendations { get; set; } = new();

    /// <summary>
    /// Performance optimization recommendations
    /// </summary>
    public List<PerformanceOptimizationRecommendation> PerformanceOptimizationRecommendations { get; set; } = new();

    /// <summary>
    /// Overall recommendation score
    /// </summary>
    public double OverallRecommendationScore { get; set; }

    /// <summary>
    /// Priority recommendations
    /// </summary>
    public List<string> PriorityRecommendations { get; set; } = new();

    /// <summary>
    /// Generated at
    /// </summary>
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Cost optimization recommendation
/// </summary>
public class CostOptimizationRecommendation
{
    /// <summary>
    /// Recommendation ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Category
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Potential savings
    /// </summary>
    public decimal PotentialSavings { get; set; }

    /// <summary>
    /// Savings percentage
    /// </summary>
    public double SavingsPercentage { get; set; }

    /// <summary>
    /// Implementation effort
    /// </summary>
    public string ImplementationEffort { get; set; } = string.Empty;

    /// <summary>
    /// Priority
    /// </summary>
    public string Priority { get; set; } = string.Empty;

    /// <summary>
    /// Implementation steps
    /// </summary>
    public List<string> ImplementationSteps { get; set; } = new();

    /// <summary>
    /// Performance optimization recommendations
    /// </summary>
    public List<PerformanceOptimizationRecommendation> PerformanceOptimizationRecommendations { get; set; } = new();

    /// <summary>
    /// Overall recommendation score
    /// </summary>
    public double OverallRecommendationScore { get; set; }

    /// <summary>
    /// Priority recommendations
    /// </summary>
    public List<string> PriorityRecommendations { get; set; } = new();

    /// <summary>
    /// Generated at
    /// </summary>
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Model recommendation
/// </summary>
public class ModelRecommendation
{
    /// <summary>
    /// Recommended model
    /// </summary>
    public string RecommendedModel { get; set; } = string.Empty;

    /// <summary>
    /// Reason
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Confidence score
    /// </summary>
    public double ConfidenceScore { get; set; }

    /// <summary>
    /// Expected performance improvement
    /// </summary>
    public double ExpectedPerformanceImprovement { get; set; }

    /// <summary>
    /// Estimated cost
    /// </summary>
    public decimal EstimatedCost { get; set; }

    /// <summary>
    /// Use cases
    /// </summary>
    public List<string> UseCases { get; set; } = new();
}

/// <summary>
/// Data recommendation
/// </summary>
public class DataRecommendation
{
    /// <summary>
    /// Recommendation type
    /// </summary>
    public string RecommendationType { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Priority
    /// </summary>
    public string Priority { get; set; } = string.Empty;

    /// <summary>
    /// Expected impact
    /// </summary>
    public string ExpectedImpact { get; set; } = string.Empty;

    /// <summary>
    /// Implementation steps
    /// </summary>
    public List<string> ImplementationSteps { get; set; } = new();

    /// <summary>
    /// Affected data aspects
    /// </summary>
    public List<string> AffectedDataAspects { get; set; } = new();
}

/// <summary>
/// Training recommendation
/// </summary>
public class TrainingRecommendation
{
    /// <summary>
    /// Parameter name
    /// </summary>
    public string ParameterName { get; set; } = string.Empty;

    /// <summary>
    /// Recommended value
    /// </summary>
    public object RecommendedValue { get; set; } = new();

    /// <summary>
    /// Current value
    /// </summary>
    public object? CurrentValue { get; set; }

    /// <summary>
    /// Reason
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Impact level
    /// </summary>
    public string ImpactLevel { get; set; } = string.Empty;

    /// <summary>
    /// Confidence level
    /// </summary>
    public double ConfidenceLevel { get; set; }
}

/// <summary>
/// Performance optimization recommendation
/// </summary>
public class PerformanceOptimizationRecommendation
{
    /// <summary>
    /// Optimization type
    /// </summary>
    public string OptimizationType { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Expected improvement
    /// </summary>
    public double ExpectedImprovement { get; set; }

    /// <summary>
    /// Implementation complexity
    /// </summary>
    public string ImplementationComplexity { get; set; } = string.Empty;

    /// <summary>
    /// Prerequisites
    /// </summary>
    public List<string> Prerequisites { get; set; } = new();

    /// <summary>
    /// Steps
    /// </summary>
    public List<string> Steps { get; set; } = new();
}

/// <summary>
/// Data quality report
/// </summary>
public class DataQualityReport
{
    /// <summary>
    /// File ID
    /// </summary>
    public string FileId { get; set; } = string.Empty;

    /// <summary>
    /// Overall quality score (0-100)
    /// </summary>
    public double OverallQualityScore { get; set; }

    /// <summary>
    /// Quality status
    /// </summary>
    public string QualityStatus { get; set; } = string.Empty;

    /// <summary>
    /// Total records
    /// </summary>
    public long TotalRecords { get; set; }

    /// <summary>
    /// Valid records
    /// </summary>
    public long ValidRecords { get; set; }

    /// <summary>
    /// Invalid records
    /// </summary>
    public long InvalidRecords { get; set; }

    /// <summary>
    /// Quality issues
    /// </summary>
    public List<DataQualityIssue> QualityIssues { get; set; } = new();

    /// <summary>
    /// Data statistics
    /// </summary>
    public DataStatistics DataStatistics { get; set; } = new();

    /// <summary>
    /// Recommendations
    /// </summary>
    public List<DataQualityRecommendation> Recommendations { get; set; } = new();

    /// <summary>
    /// Validation rules applied
    /// </summary>
    public List<string> ValidationRulesApplied { get; set; } = new();

    /// <summary>
    /// Generated at
    /// </summary>
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Data quality issue
/// </summary>
public class DataQualityIssue
{
    /// <summary>
    /// Issue type
    /// </summary>
    public string IssueType { get; set; } = string.Empty;

    /// <summary>
    /// Severity
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Affected records count
    /// </summary>
    public long AffectedRecordsCount { get; set; }

    /// <summary>
    /// Sample affected records
    /// </summary>
    public List<object> SampleAffectedRecords { get; set; } = new();

    /// <summary>
    /// Suggested fix
    /// </summary>
    public string SuggestedFix { get; set; } = string.Empty;

    /// <summary>
    /// Auto-fixable
    /// </summary>
    public bool AutoFixable { get; set; }
}

/// <summary>
/// Data statistics
/// </summary>
public class DataStatistics
{
    /// <summary>
    /// Average input length
    /// </summary>
    public double AverageInputLength { get; set; }

    /// <summary>
    /// Average output length
    /// </summary>
    public double AverageOutputLength { get; set; }

    /// <summary>
    /// Input length distribution
    /// </summary>
    public Dictionary<string, int> InputLengthDistribution { get; set; } = new();

    /// <summary>
    /// Output length distribution
    /// </summary>
    public Dictionary<string, int> OutputLengthDistribution { get; set; } = new();

    /// <summary>
    /// Unique inputs
    /// </summary>
    public long UniqueInputs { get; set; }

    /// <summary>
    /// Duplicate records
    /// </summary>
    public long DuplicateRecords { get; set; }

    /// <summary>
    /// Language distribution
    /// </summary>
    public Dictionary<string, int> LanguageDistribution { get; set; } = new();

    /// <summary>
    /// Content categories
    /// </summary>
    public Dictionary<string, int> ContentCategories { get; set; } = new();
}

/// <summary>
/// Data quality recommendation
/// </summary>
public class DataQualityRecommendation
{
    /// <summary>
    /// Recommendation type
    /// </summary>
    public string RecommendationType { get; set; } = string.Empty;

    /// <summary>
    /// Title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Priority
    /// </summary>
    public string Priority { get; set; } = string.Empty;

    /// <summary>
    /// Expected impact
    /// </summary>
    public string ExpectedImpact { get; set; } = string.Empty;

    /// <summary>
    /// Implementation steps
    /// </summary>
    public List<string> ImplementationSteps { get; set; } = new();

    /// <summary>
    /// Effort required
    /// </summary>
    public string EffortRequired { get; set; } = string.Empty;
}

/// <summary>
/// Fine-tuning template
/// </summary>
public class FineTuningTemplate
{
    /// <summary>
    /// Template ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Template name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Category
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Use case
    /// </summary>
    public string UseCase { get; set; } = string.Empty;

    /// <summary>
    /// Base model
    /// </summary>
    public string BaseModel { get; set; } = string.Empty;

    /// <summary>
    /// Recommended provider
    /// </summary>
    public string RecommendedProvider { get; set; } = string.Empty;

    /// <summary>
    /// Training parameters
    /// </summary>
    public Dictionary<string, object> TrainingParameters { get; set; } = new();

    /// <summary>
    /// Data requirements
    /// </summary>
    public DataRequirements DataRequirements { get; set; } = new();

    /// <summary>
    /// Expected performance
    /// </summary>
    public ExpectedPerformance ExpectedPerformance { get; set; } = new();

    /// <summary>
    /// Estimated cost range
    /// </summary>
    public CostRange EstimatedCostRange { get; set; } = new();

    /// <summary>
    /// Estimated training time
    /// </summary>
    public TimeSpan EstimatedTrainingTime { get; set; }

    /// <summary>
    /// Difficulty level
    /// </summary>
    public string DifficultyLevel { get; set; } = string.Empty;

    /// <summary>
    /// Prerequisites
    /// </summary>
    public List<string> Prerequisites { get; set; } = new();

    /// <summary>
    /// Tags
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Created at
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Updated at
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Usage count
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// Success rate
    /// </summary>
    public double SuccessRate { get; set; }
}

/// <summary>
/// Data requirements
/// </summary>
public class DataRequirements
{
    /// <summary>
    /// Minimum examples
    /// </summary>
    public int MinimumExamples { get; set; }

    /// <summary>
    /// Recommended examples
    /// </summary>
    public int RecommendedExamples { get; set; }

    /// <summary>
    /// Data format
    /// </summary>
    public string DataFormat { get; set; } = string.Empty;

    /// <summary>
    /// Required fields
    /// </summary>
    public List<string> RequiredFields { get; set; } = new();

    /// <summary>
    /// Data quality requirements
    /// </summary>
    public List<string> QualityRequirements { get; set; } = new();

    /// <summary>
    /// Example data structure
    /// </summary>
    public object ExampleDataStructure { get; set; } = new();
}

/// <summary>
/// Expected performance
/// </summary>
public class ExpectedPerformance
{
    /// <summary>
    /// Performance metrics
    /// </summary>
    public Dictionary<string, double> PerformanceMetrics { get; set; } = new();

    /// <summary>
    /// Improvement over base model
    /// </summary>
    public double ImprovementOverBaseModel { get; set; }

    /// <summary>
    /// Confidence level
    /// </summary>
    public double ConfidenceLevel { get; set; }

    /// <summary>
    /// Benchmark results
    /// </summary>
    public List<BenchmarkResult> BenchmarkResults { get; set; } = new();
}

/// <summary>
/// Cost range
/// </summary>
public class CostRange
{
    /// <summary>
    /// Minimum cost
    /// </summary>
    public decimal MinimumCost { get; set; }

    /// <summary>
    /// Maximum cost
    /// </summary>
    public decimal MaximumCost { get; set; }

    /// <summary>
    /// Average cost
    /// </summary>
    public decimal AverageCost { get; set; }

    /// <summary>
    /// Currency
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Cost factors
    /// </summary>
    public List<string> CostFactors { get; set; } = new();
}

/// <summary>
/// Benchmark result
/// </summary>
public class BenchmarkResult
{
    /// <summary>
    /// Benchmark name
    /// </summary>
    public string BenchmarkName { get; set; } = string.Empty;

    /// <summary>
    /// Score
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Metric
    /// </summary>
    public string Metric { get; set; } = string.Empty;

    /// <summary>
    /// Dataset
    /// </summary>
    public string Dataset { get; set; } = string.Empty;

    /// <summary>
    /// Date tested
    /// </summary>
    public DateTime DateTested { get; set; }
}

/// <summary>
/// Create job from template request
/// </summary>
public class CreateJobFromTemplateRequest
{
    /// <summary>
    /// Template ID
    /// </summary>
    public string TemplateId { get; set; } = string.Empty;

    /// <summary>
    /// Job name
    /// </summary>
    public string JobName { get; set; } = string.Empty;

    /// <summary>
    /// Training file ID
    /// </summary>
    public string TrainingFileId { get; set; } = string.Empty;

    /// <summary>
    /// Validation file ID (optional)
    /// </summary>
    public string? ValidationFileId { get; set; }

    /// <summary>
    /// Parameter overrides
    /// </summary>
    public Dictionary<string, object> ParameterOverrides { get; set; } = new();

    /// <summary>
    /// Custom suffix
    /// </summary>
    public string? CustomSuffix { get; set; }

    /// <summary>
    /// Tags
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Notification settings
    /// </summary>
    public NotificationSettings? NotificationSettings { get; set; }

    /// <summary>
    /// Auto-deploy on completion
    /// </summary>
    public bool AutoDeployOnCompletion { get; set; } = false;

    /// <summary>
    /// Budget limit
    /// </summary>
    public decimal? BudgetLimit { get; set; }
}

/// <summary>
/// Notification settings
/// </summary>
public class NotificationSettings
{
    /// <summary>
    /// Email notifications
    /// </summary>
    public bool EmailNotifications { get; set; } = true;

    /// <summary>
    /// Webhook URL
    /// </summary>
    public string? WebhookUrl { get; set; }

    /// <summary>
    /// Notification events
    /// </summary>
    public List<string> NotificationEvents { get; set; } = new();

    /// <summary>
    /// Recipients
    /// </summary>
    public List<string> Recipients { get; set; } = new();
}

/// <summary>
/// Fine-tuning job insights
/// </summary>
public class FineTuningJobInsights
{
    /// <summary>
    /// Job ID
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Performance insights
    /// </summary>
    public PerformanceInsights PerformanceInsights { get; set; } = new();

    /// <summary>
    /// Training insights
    /// </summary>
    public TrainingInsights TrainingInsights { get; set; } = new();

    /// <summary>
    /// Cost insights
    /// </summary>
    public CostInsights CostInsights { get; set; } = new();

    /// <summary>
    /// Data insights
    /// </summary>
    public DataInsights DataInsights { get; set; } = new();

    /// <summary>
    /// Model insights
    /// </summary>
    public ModelInsights ModelInsights { get; set; } = new();

    /// <summary>
    /// Recommendations
    /// </summary>
    public List<JobInsightRecommendation> Recommendations { get; set; } = new();

    /// <summary>
    /// Key findings
    /// </summary>
    public List<string> KeyFindings { get; set; } = new();

    /// <summary>
    /// Success factors
    /// </summary>
    public List<string> SuccessFactors { get; set; } = new();

    /// <summary>
    /// Areas for improvement
    /// </summary>
    public List<string> AreasForImprovement { get; set; } = new();

    /// <summary>
    /// Generated at
    /// </summary>
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Performance insights
/// </summary>
public class PerformanceInsights
{
    /// <summary>
    /// Overall performance score
    /// </summary>
    public double OverallPerformanceScore { get; set; }

    /// <summary>
    /// Performance vs baseline
    /// </summary>
    public double PerformanceVsBaseline { get; set; }

    /// <summary>
    /// Best performing epochs
    /// </summary>
    public List<int> BestPerformingEpochs { get; set; } = new();

    /// <summary>
    /// Performance bottlenecks
    /// </summary>
    public List<string> PerformanceBottlenecks { get; set; } = new();

    /// <summary>
    /// Performance trends
    /// </summary>
    public List<PerformanceTrendPoint> PerformanceTrends { get; set; } = new();
}

/// <summary>
/// Performance trend point
/// </summary>
public class PerformanceTrendPoint
{
    /// <summary>
    /// Date
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Performance score
    /// </summary>
    public double PerformanceScore { get; set; }

    /// <summary>
    /// Response time
    /// </summary>
    public double ResponseTime { get; set; }

    /// <summary>
    /// Success rate
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Throughput
    /// </summary>
    public double Throughput { get; set; }

    /// <summary>
    /// Error rate
    /// </summary>
    public double ErrorRate { get; set; }

    /// <summary>
    /// Best performing epochs
    /// </summary>
    public List<int> BestPerformingEpochs { get; set; } = new();

    /// <summary>
    /// Performance bottlenecks
    /// </summary>
    public List<string> PerformanceBottlenecks { get; set; } = new();
}

/// <summary>
/// Training insights
/// </summary>
public class TrainingInsights
{
    /// <summary>
    /// Training efficiency score
    /// </summary>
    public double TrainingEfficiencyScore { get; set; }

    /// <summary>
    /// Convergence analysis
    /// </summary>
    public ConvergenceAnalysis ConvergenceAnalysis { get; set; } = new();

    /// <summary>
    /// Learning rate analysis
    /// </summary>
    public LearningRateAnalysis LearningRateAnalysis { get; set; } = new();

    /// <summary>
    /// Overfitting indicators
    /// </summary>
    public List<string> OverfittingIndicators { get; set; } = new();

    /// <summary>
    /// Training stability
    /// </summary>
    public double TrainingStability { get; set; }
}

/// <summary>
/// Convergence analysis
/// </summary>
public class ConvergenceAnalysis
{
    /// <summary>
    /// Converged
    /// </summary>
    public bool Converged { get; set; }

    /// <summary>
    /// Convergence epoch
    /// </summary>
    public int? ConvergenceEpoch { get; set; }

    /// <summary>
    /// Convergence rate
    /// </summary>
    public double ConvergenceRate { get; set; }

    /// <summary>
    /// Early stopping triggered
    /// </summary>
    public bool EarlyStoppingTriggered { get; set; }
}

/// <summary>
/// Learning rate analysis
/// </summary>
public class LearningRateAnalysis
{
    /// <summary>
    /// Optimal learning rate
    /// </summary>
    public double OptimalLearningRate { get; set; }

    /// <summary>
    /// Learning rate effectiveness
    /// </summary>
    public double LearningRateEffectiveness { get; set; }

    /// <summary>
    /// Learning rate recommendations
    /// </summary>
    public List<string> LearningRateRecommendations { get; set; } = new();
}

/// <summary>
/// Cost insights
/// </summary>
public class CostInsights
{
    /// <summary>
    /// Total cost
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Cost efficiency score
    /// </summary>
    public double CostEfficiencyScore { get; set; }

    /// <summary>
    /// Cost vs budget
    /// </summary>
    public decimal CostVsBudget { get; set; }

    /// <summary>
    /// Cost optimization opportunities
    /// </summary>
    public List<string> CostOptimizationOpportunities { get; set; } = new();

    /// <summary>
    /// Cost breakdown
    /// </summary>
    public Dictionary<string, decimal> CostBreakdown { get; set; } = new();
}

/// <summary>
/// Data insights
/// </summary>
public class DataInsights
{
    /// <summary>
    /// Data quality score
    /// </summary>
    public double DataQualityScore { get; set; }

    /// <summary>
    /// Data utilization efficiency
    /// </summary>
    public double DataUtilizationEfficiency { get; set; }

    /// <summary>
    /// Data diversity score
    /// </summary>
    public double DataDiversityScore { get; set; }

    /// <summary>
    /// Data recommendations
    /// </summary>
    public List<string> DataRecommendations { get; set; } = new();

    /// <summary>
    /// Data issues identified
    /// </summary>
    public List<string> DataIssuesIdentified { get; set; } = new();
}

/// <summary>
/// Model insights
/// </summary>
public class ModelInsights
{
    /// <summary>
    /// Model complexity score
    /// </summary>
    public double ModelComplexityScore { get; set; }

    /// <summary>
    /// Model generalization ability
    /// </summary>
    public double ModelGeneralizationAbility { get; set; }

    /// <summary>
    /// Model robustness score
    /// </summary>
    public double ModelRobustnessScore { get; set; }

    /// <summary>
    /// Model interpretability
    /// </summary>
    public double ModelInterpretability { get; set; }

    /// <summary>
    /// Model recommendations
    /// </summary>
    public List<string> ModelRecommendations { get; set; } = new();
}

/// <summary>
/// Job insight recommendation
/// </summary>
public class JobInsightRecommendation
{
    /// <summary>
    /// Category
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Priority
    /// </summary>
    public string Priority { get; set; } = string.Empty;

    /// <summary>
    /// Impact level
    /// </summary>
    public string ImpactLevel { get; set; } = string.Empty;

    /// <summary>
    /// Implementation effort
    /// </summary>
    public string ImplementationEffort { get; set; } = string.Empty;

    /// <summary>
    /// Action items
    /// </summary>
    public List<string> ActionItems { get; set; } = new();
}

/// <summary>
/// Model usage stats
/// </summary>
public class ModelUsageStats
{
    /// <summary>
    /// Model ID
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Provider
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Request count
    /// </summary>
    public long RequestCount { get; set; }

    /// <summary>
    /// Token count
    /// </summary>
    public long TokenCount { get; set; }

    /// <summary>
    /// Cost
    /// </summary>
    public decimal Cost { get; set; }

    /// <summary>
    /// Percentage of total usage
    /// </summary>
    public double PercentageOfTotal { get; set; }
}

/// <summary>
/// Cost trend point
/// </summary>
public class CostTrendPoint
{
    /// <summary>
    /// Date
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Cost
    /// </summary>
    public decimal Cost { get; set; }

    /// <summary>
    /// Trend direction
    /// </summary>
    public string TrendDirection { get; set; } = string.Empty;

    /// <summary>
    /// Change percentage
    /// </summary>
    public double ChangePercentage { get; set; }
}

/// <summary>
/// Fine-tuning performance metrics
/// </summary>
public class FineTuningPerformanceMetrics
{
    /// <summary>
    /// Average accuracy
    /// </summary>
    public double AverageAccuracy { get; set; }

    /// <summary>
    /// Average loss
    /// </summary>
    public double AverageLoss { get; set; }

    /// <summary>
    /// Average F1 score
    /// </summary>
    public double AverageF1Score { get; set; }

    /// <summary>
    /// Best performing job
    /// </summary>
    public string? BestPerformingJob { get; set; }

    /// <summary>
    /// Worst performing job
    /// </summary>
    public string? WorstPerformingJob { get; set; }

    /// <summary>
    /// Performance distribution
    /// </summary>
    public Dictionary<string, double> PerformanceDistribution { get; set; } = new();
}

/// <summary>
/// Export format
/// </summary>
public enum ExportFormat
{
    /// <summary>
    /// CSV format
    /// </summary>
    Csv,

    /// <summary>
    /// JSON format
    /// </summary>
    Json,

    /// <summary>
    /// Excel format
    /// </summary>
    Excel,

    /// <summary>
    /// PDF format
    /// </summary>
    Pdf
}
