namespace LLMGateway.Core.Models.Routing;

/// <summary>
/// A/B testing experiment
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
    /// User segments to include
    /// </summary>
    public List<string> UserSegments { get; set; } = new();
    
    /// <summary>
    /// Metrics to track
    /// </summary>
    public List<string> Metrics { get; set; } = new();
    
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
}

/// <summary>
/// A/B testing experiment result
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
    /// Metrics
    /// </summary>
    public Dictionary<string, double> Metrics { get; set; } = new();
}

/// <summary>
/// A/B testing experiment create request
/// </summary>
public class ABTestingExperimentCreateRequest
{
    /// <summary>
    /// Experiment name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Experiment description
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
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
    /// User segments to include
    /// </summary>
    public List<string>? UserSegments { get; set; }
    
    /// <summary>
    /// Metrics to track
    /// </summary>
    public List<string>? Metrics { get; set; }
    
    /// <summary>
    /// End date
    /// </summary>
    public DateTime? EndDate { get; set; }
}

/// <summary>
/// A/B testing experiment update request
/// </summary>
public class ABTestingExperimentUpdateRequest
{
    /// <summary>
    /// Experiment name
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Experiment description
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Whether the experiment is active
    /// </summary>
    public bool? IsActive { get; set; }
    
    /// <summary>
    /// Traffic allocation percentage (0-100)
    /// </summary>
    public int? TrafficAllocationPercentage { get; set; }
    
    /// <summary>
    /// Control group model ID
    /// </summary>
    public string? ControlModelId { get; set; }
    
    /// <summary>
    /// Treatment group model ID
    /// </summary>
    public string? TreatmentModelId { get; set; }
    
    /// <summary>
    /// User segments to include
    /// </summary>
    public List<string>? UserSegments { get; set; }
    
    /// <summary>
    /// Metrics to track
    /// </summary>
    public List<string>? Metrics { get; set; }
    
    /// <summary>
    /// End date
    /// </summary>
    public DateTime? EndDate { get; set; }
}

/// <summary>
/// A/B testing result create request
/// </summary>
public class ABTestingResultCreateRequest
{
    /// <summary>
    /// Experiment ID
    /// </summary>
    public string ExperimentId { get; set; } = string.Empty;
    
    /// <summary>
    /// Request ID
    /// </summary>
    public string RequestId { get; set; } = string.Empty;
    
    /// <summary>
    /// Metrics
    /// </summary>
    public Dictionary<string, double> Metrics { get; set; } = new();
}

/// <summary>
/// A/B testing experiment statistics
/// </summary>
public class ABTestingExperimentStatistics
{
    /// <summary>
    /// Experiment ID
    /// </summary>
    public string ExperimentId { get; set; } = string.Empty;
    
    /// <summary>
    /// Experiment name
    /// </summary>
    public string ExperimentName { get; set; } = string.Empty;
    
    /// <summary>
    /// Control group sample size
    /// </summary>
    public int ControlSampleSize { get; set; }
    
    /// <summary>
    /// Treatment group sample size
    /// </summary>
    public int TreatmentSampleSize { get; set; }
    
    /// <summary>
    /// Metrics statistics
    /// </summary>
    public Dictionary<string, MetricStatistics> Metrics { get; set; } = new();
}

/// <summary>
/// Metric statistics
/// </summary>
public class MetricStatistics
{
    /// <summary>
    /// Metric name
    /// </summary>
    public string MetricName { get; set; } = string.Empty;
    
    /// <summary>
    /// Control group average
    /// </summary>
    public double ControlAverage { get; set; }
    
    /// <summary>
    /// Treatment group average
    /// </summary>
    public double TreatmentAverage { get; set; }
    
    /// <summary>
    /// Percentage difference
    /// </summary>
    public double PercentageDifference { get; set; }
    
    /// <summary>
    /// P-value
    /// </summary>
    public double? PValue { get; set; }
    
    /// <summary>
    /// Is statistically significant
    /// </summary>
    public bool? IsStatisticallySignificant { get; set; }
}
