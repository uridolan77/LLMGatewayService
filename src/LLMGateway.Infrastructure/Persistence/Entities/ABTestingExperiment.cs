using System.Text.Json;

namespace LLMGateway.Infrastructure.Persistence.Entities;

/// <summary>
/// A/B testing experiment entity
/// </summary>
public class ABTestingExperiment
{
    /// <summary>
    /// Experiment ID
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Experiment name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Experiment description
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the experiment is active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Start date
    /// </summary>
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// End date
    /// </summary>
    public DateTime? EndDate { get; set; }
    
    /// <summary>
    /// Traffic allocation percentage (0-100)
    /// </summary>
    public int TrafficAllocationPercentage { get; set; } = 50;
    
    /// <summary>
    /// Control group model ID
    /// </summary>
    public string ControlModelId { get; set; } = string.Empty;
    
    /// <summary>
    /// Treatment group model ID
    /// </summary>
    public string TreatmentModelId { get; set; } = string.Empty;
    
    /// <summary>
    /// User segments to include (stored as JSON)
    /// </summary>
    public string UserSegmentsJson { get; set; } = "[]";
    
    /// <summary>
    /// Metrics to track (stored as JSON)
    /// </summary>
    public string MetricsJson { get; set; } = "[]";
    
    /// <summary>
    /// Created by
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Created at
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Updated at
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Navigation property for results
    /// </summary>
    public virtual ICollection<ABTestingResult> Results { get; set; } = new List<ABTestingResult>();
    
    /// <summary>
    /// Navigation property for user assignments
    /// </summary>
    public virtual ICollection<ABTestingUserAssignment> UserAssignments { get; set; } = new List<ABTestingUserAssignment>();
    
    /// <summary>
    /// Get user segments as list
    /// </summary>
    /// <returns>List of user segments</returns>
    public List<string> GetUserSegments()
    {
        if (string.IsNullOrEmpty(UserSegmentsJson))
        {
            return new List<string>();
        }
        
        try
        {
            return JsonSerializer.Deserialize<List<string>>(UserSegmentsJson) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
    
    /// <summary>
    /// Get metrics as list
    /// </summary>
    /// <returns>List of metrics</returns>
    public List<string> GetMetrics()
    {
        if (string.IsNullOrEmpty(MetricsJson))
        {
            return new List<string>();
        }
        
        try
        {
            return JsonSerializer.Deserialize<List<string>>(MetricsJson) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
    
    /// <summary>
    /// Convert to domain model
    /// </summary>
    /// <returns>Domain model</returns>
    public Core.Models.Routing.ABTestingExperiment ToDomainModel()
    {
        return new Core.Models.Routing.ABTestingExperiment
        {
            Id = Id,
            Name = Name,
            Description = Description,
            IsActive = IsActive,
            StartDate = StartDate,
            EndDate = EndDate,
            TrafficAllocationPercentage = TrafficAllocationPercentage,
            ControlModelId = ControlModelId,
            TreatmentModelId = TreatmentModelId,
            UserSegments = GetUserSegments(),
            Metrics = GetMetrics(),
            CreatedBy = CreatedBy,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt
        };
    }
}
