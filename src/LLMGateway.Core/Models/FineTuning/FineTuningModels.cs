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
