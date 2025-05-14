using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LLMGateway.Tuning.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace LLMGateway.Tuning.Monitoring.Performance
{
    public class PerformanceAnalyzer : IPerformanceAnalyzer
    {
        private readonly ILogger<PerformanceAnalyzer> _logger;
        private readonly IFeedbackRepository _feedbackRepository;

        public PerformanceAnalyzer(
            ILogger<PerformanceAnalyzer> logger,
            IFeedbackRepository feedbackRepository)
        {
            _logger = logger;
            _feedbackRepository = feedbackRepository;
        }

        public async Task<Dictionary<string, double>> CalculateMetricsAsync(string modelId, DateTime since)
        {
            try
            {
                _logger.LogInformation("Calculating performance metrics for model {ModelId} since {Since}", 
                    modelId, since);

                // Get feedback for the model
                var feedback = await _feedbackRepository.GetFeedbackForDatasetGenerationAsync(
                    since, 
                    maxRecords: 10000);
                
                // Filter feedback for the specific model
                var modelFeedback = feedback.Where(f => 
                    f.Metadata.TryGetValue("ModelId", out var id) && id == modelId).ToList();
                
                _logger.LogInformation("Found {Count} feedback records for model {ModelId}", 
                    modelFeedback.Count, modelId);
                
                if (modelFeedback.Count == 0)
                {
                    return new Dictionary<string, double>();
                }

                // Calculate metrics
                var metrics = new Dictionary<string, double>
                {
                    // User satisfaction score (average)
                    ["averageSatisfaction"] = modelFeedback.Average(f => f.SatisfactionScore),
                    
                    // Percentage of thumbs up feedback
                    ["thumbsUpRate"] = modelFeedback.Count(f => f.FeedbackType == Core.Enums.FeedbackType.ThumbsUp) / 
                                      (double)modelFeedback.Count,
                    
                    // Percentage of thumbs down feedback
                    ["thumbsDownRate"] = modelFeedback.Count(f => f.FeedbackType == Core.Enums.FeedbackType.ThumbsDown) / 
                                        (double)modelFeedback.Count,
                    
                    // Average processing time
                    ["averageProcessingTime"] = modelFeedback.Average(f => f.RequestContext?.ProcessingTimeMs ?? 0),
                    
                    // Average response length (in tokens)
                    ["averageResponseTokens"] = modelFeedback.Average(f => f.RequestContext?.ResponseTokens ?? 0)
                };
                
                _logger.LogInformation("Calculated {Count} performance metrics for model {ModelId}", 
                    metrics.Count, modelId);
                
                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating performance metrics for model {ModelId}", modelId);
                return new Dictionary<string, double>();
            }
        }

        public async Task<bool> IsModelPerformingWellAsync(string modelId, Dictionary<string, double> thresholds)
        {
            try
            {
                // Get current metrics
                var metrics = await CalculateMetricsAsync(modelId, DateTime.UtcNow.AddDays(-7));
                
                if (metrics.Count == 0)
                {
                    _logger.LogWarning("No metrics available for model {ModelId}", modelId);
                    return false;
                }
                
                // Check if all thresholds are met
                bool isPerformingWell = true;
                var failedThresholds = new List<string>();
                
                foreach (var threshold in thresholds)
                {
                    if (metrics.TryGetValue(threshold.Key, out double value))
                    {
                        if (value < threshold.Value)
                        {
                            isPerformingWell = false;
                            failedThresholds.Add($"{threshold.Key}: {value:F2} < {threshold.Value:F2}");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Metric {Metric} not available for model {ModelId}", 
                            threshold.Key, modelId);
                    }
                }
                
                if (isPerformingWell)
                {
                    _logger.LogInformation("Model {ModelId} is performing well, all thresholds met", modelId);
                }
                else
                {
                    _logger.LogWarning("Model {ModelId} is not performing well. Failed thresholds: {Thresholds}", 
                        modelId, string.Join(", ", failedThresholds));
                }
                
                return isPerformingWell;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if model {ModelId} is performing well", modelId);
                return false;
            }
        }
    }
}
