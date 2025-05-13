namespace LLMGateway.Infrastructure.Persistence.Entities;

/// <summary>
/// Fine-tuning step metric entity
/// </summary>
public class FineTuningStepMetric
{
    /// <summary>
    /// Metric ID
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Job ID
    /// </summary>
    public string JobId { get; set; } = string.Empty;
    
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
    
    /// <summary>
    /// Navigation property for job
    /// </summary>
    public virtual FineTuningJob Job { get; set; } = null!;
    
    /// <summary>
    /// Convert to domain model
    /// </summary>
    /// <returns>Domain model</returns>
    public Core.Models.FineTuning.FineTuningStepMetric ToDomainModel()
    {
        return new Core.Models.FineTuning.FineTuningStepMetric
        {
            Step = Step,
            Timestamp = Timestamp,
            Loss = Loss,
            Accuracy = Accuracy,
            ElapsedTokens = ElapsedTokens
        };
    }
    
    /// <summary>
    /// Create from domain model
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <param name="model">Domain model</param>
    /// <returns>Entity</returns>
    public static FineTuningStepMetric FromDomainModel(string jobId, Core.Models.FineTuning.FineTuningStepMetric model)
    {
        return new FineTuningStepMetric
        {
            Id = Guid.NewGuid().ToString(),
            JobId = jobId,
            Step = model.Step,
            Timestamp = model.Timestamp,
            Loss = model.Loss,
            Accuracy = model.Accuracy,
            ElapsedTokens = model.ElapsedTokens
        };
    }
}
