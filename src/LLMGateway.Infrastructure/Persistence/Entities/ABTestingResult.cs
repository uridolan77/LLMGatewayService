using System.Text.Json;

namespace LLMGateway.Infrastructure.Persistence.Entities;

/// <summary>
/// A/B testing result entity
/// </summary>
public class ABTestingResult
{
    /// <summary>
    /// Result ID
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
    /// Request ID
    /// </summary>
    public string RequestId { get; set; } = string.Empty;
    
    /// <summary>
    /// Group (control or treatment)
    /// </summary>
    public string Group { get; set; } = string.Empty;
    
    /// <summary>
    /// Model ID
    /// </summary>
    public string ModelId { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Metrics (stored as JSON)
    /// </summary>
    public string MetricsJson { get; set; } = "{}";
    
    /// <summary>
    /// Navigation property for experiment
    /// </summary>
    public virtual ABTestingExperiment Experiment { get; set; } = null!;
    
    /// <summary>
    /// Get metrics as dictionary
    /// </summary>
    /// <returns>Metrics dictionary</returns>
    public Dictionary<string, double> GetMetrics()
    {
        if (string.IsNullOrEmpty(MetricsJson))
        {
            return new Dictionary<string, double>();
        }
        
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, double>>(MetricsJson) ?? new Dictionary<string, double>();
        }
        catch
        {
            return new Dictionary<string, double>();
        }
    }
    
    /// <summary>
    /// Convert to domain model
    /// </summary>
    /// <returns>Domain model</returns>
    public Core.Models.Routing.ABTestingResult ToDomainModel()
    {
        return new Core.Models.Routing.ABTestingResult
        {
            Id = Id,
            ExperimentId = ExperimentId,
            UserId = UserId,
            RequestId = RequestId,
            Group = Group,
            ModelId = ModelId,
            Timestamp = Timestamp,
            Metrics = GetMetrics()
        };
    }
}
