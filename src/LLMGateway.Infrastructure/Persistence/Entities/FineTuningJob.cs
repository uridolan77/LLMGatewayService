using System.Text.Json;
using LLMGateway.Core.Models.FineTuning;

namespace LLMGateway.Infrastructure.Persistence.Entities;

/// <summary>
/// Fine-tuning job entity
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
    /// Hyperparameters (stored as JSON)
    /// </summary>
    public string HyperparametersJson { get; set; } = "{}";
    
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
    /// Metrics (stored as JSON)
    /// </summary>
    public string? MetricsJson { get; set; }
    
    /// <summary>
    /// Provider-specific job ID
    /// </summary>
    public string? ProviderJobId { get; set; }
    
    /// <summary>
    /// Tags (stored as JSON)
    /// </summary>
    public string TagsJson { get; set; } = "[]";
    
    /// <summary>
    /// Navigation property for events
    /// </summary>
    public virtual ICollection<FineTuningStepMetric> Events { get; set; } = new List<FineTuningStepMetric>();
    
    /// <summary>
    /// Get hyperparameters
    /// </summary>
    /// <returns>Hyperparameters</returns>
    public Core.Models.FineTuning.FineTuningHyperparameters GetHyperparameters()
    {
        if (string.IsNullOrEmpty(HyperparametersJson))
        {
            return new Core.Models.FineTuning.FineTuningHyperparameters();
        }
        
        try
        {
            return JsonSerializer.Deserialize<Core.Models.FineTuning.FineTuningHyperparameters>(HyperparametersJson) 
                ?? new Core.Models.FineTuning.FineTuningHyperparameters();
        }
        catch
        {
            return new Core.Models.FineTuning.FineTuningHyperparameters();
        }
    }
    
    /// <summary>
    /// Set hyperparameters
    /// </summary>
    /// <param name="hyperparameters">Hyperparameters</param>
    public void SetHyperparameters(Core.Models.FineTuning.FineTuningHyperparameters hyperparameters)
    {
        HyperparametersJson = JsonSerializer.Serialize(hyperparameters);
    }
    
    /// <summary>
    /// Get metrics
    /// </summary>
    /// <returns>Metrics</returns>
    public Core.Models.FineTuning.FineTuningMetrics? GetMetrics()
    {
        if (string.IsNullOrEmpty(MetricsJson))
        {
            return null;
        }
        
        try
        {
            return JsonSerializer.Deserialize<Core.Models.FineTuning.FineTuningMetrics>(MetricsJson);
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Set metrics
    /// </summary>
    /// <param name="metrics">Metrics</param>
    public void SetMetrics(Core.Models.FineTuning.FineTuningMetrics? metrics)
    {
        MetricsJson = metrics != null ? JsonSerializer.Serialize(metrics) : null;
    }
    
    /// <summary>
    /// Get tags
    /// </summary>
    /// <returns>Tags</returns>
    public List<string> GetTags()
    {
        if (string.IsNullOrEmpty(TagsJson))
        {
            return new List<string>();
        }
        
        try
        {
            return JsonSerializer.Deserialize<List<string>>(TagsJson) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
    
    /// <summary>
    /// Set tags
    /// </summary>
    /// <param name="tags">Tags</param>
    public void SetTags(List<string> tags)
    {
        TagsJson = JsonSerializer.Serialize(tags);
    }
    
    /// <summary>
    /// Convert to domain model
    /// </summary>
    /// <returns>Domain model</returns>
    public Core.Models.FineTuning.FineTuningJob ToDomainModel()
    {
        return new Core.Models.FineTuning.FineTuningJob
        {
            Id = Id,
            Name = Name,
            Description = Description,
            Provider = Provider,
            BaseModelId = BaseModelId,
            FineTunedModelId = FineTunedModelId,
            TrainingFileId = TrainingFileId,
            ValidationFileId = ValidationFileId,
            Hyperparameters = GetHyperparameters(),
            Status = Status,
            CreatedBy = CreatedBy,
            CreatedAt = CreatedAt,
            StartedAt = StartedAt,
            CompletedAt = CompletedAt,
            ErrorMessage = ErrorMessage,
            Metrics = GetMetrics(),
            ProviderJobId = ProviderJobId,
            Tags = GetTags()
        };
    }
    
    /// <summary>
    /// Create from domain model
    /// </summary>
    /// <param name="model">Domain model</param>
    /// <returns>Entity</returns>
    public static FineTuningJob FromDomainModel(Core.Models.FineTuning.FineTuningJob model)
    {
        var entity = new FineTuningJob
        {
            Id = model.Id,
            Name = model.Name,
            Description = model.Description,
            Provider = model.Provider,
            BaseModelId = model.BaseModelId,
            FineTunedModelId = model.FineTunedModelId,
            TrainingFileId = model.TrainingFileId,
            ValidationFileId = model.ValidationFileId,
            Status = model.Status,
            CreatedBy = model.CreatedBy,
            CreatedAt = model.CreatedAt,
            StartedAt = model.StartedAt,
            CompletedAt = model.CompletedAt,
            ErrorMessage = model.ErrorMessage,
            ProviderJobId = model.ProviderJobId
        };
        
        entity.SetHyperparameters(model.Hyperparameters);
        entity.SetMetrics(model.Metrics);
        entity.SetTags(model.Tags);
        
        return entity;
    }
}
