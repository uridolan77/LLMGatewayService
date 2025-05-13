namespace LLMGateway.Infrastructure.Persistence.Entities;

/// <summary>
/// A/B testing user assignment entity
/// </summary>
public class ABTestingUserAssignment
{
    /// <summary>
    /// Assignment ID
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Experiment ID
    /// </summary>
    public string ExperimentId { get; set; } = string.Empty;
    
    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Group (control or treatment)
    /// </summary>
    public string Group { get; set; } = string.Empty;
    
    /// <summary>
    /// Assigned at
    /// </summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Navigation property for experiment
    /// </summary>
    public virtual ABTestingExperiment Experiment { get; set; } = null!;
}
