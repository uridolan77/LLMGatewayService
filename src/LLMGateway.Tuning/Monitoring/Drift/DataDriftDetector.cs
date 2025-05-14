using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LLMGateway.Tuning.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace LLMGateway.Tuning.Monitoring.Drift
{
    public class DataDriftDetector
    {
        private readonly ILogger<DataDriftDetector> _logger;
        private readonly IFeedbackRepository _feedbackRepository;
        private readonly IDatasetStorage _datasetStorage;

        public DataDriftDetector(
            ILogger<DataDriftDetector> logger,
            IFeedbackRepository feedbackRepository,
            IDatasetStorage datasetStorage)
        {
            _logger = logger;
            _feedbackRepository = feedbackRepository;
            _datasetStorage = datasetStorage;
        }

        public async Task<DriftDetectionResult> DetectDriftAsync(string referenceDatasetId, DateTime since)
        {
            try
            {
                _logger.LogInformation("Detecting data drift between dataset {DatasetId} and recent feedback since {Since}", 
                    referenceDatasetId, since);

                // Load reference dataset statistics (or calculate them)
                var referenceStats = await GetDatasetStatisticsAsync(referenceDatasetId);
                
                // Get recent feedback
                var recentFeedback = await _feedbackRepository.GetFeedbackForDatasetGenerationAsync(since);
                
                // Calculate statistics for recent feedback
                var recentStats = CalculateStatisticsFromFeedback(recentFeedback);
                
                // Calculate drift metrics
                var featureDrift = CalculateFeatureDrift(referenceStats, recentStats);
                
                // Calculate distribution shift
                var distributionShift = CalculateDistributionShift(
                    referenceStats.TopicDistribution, 
                    recentStats.TopicDistribution);
                
                // Calculate overall drift score
                var overallDriftScore = CalculateOverallDriftScore(featureDrift, distributionShift);
                
                // Detect data quality issues
                var qualityIssues = DetectQualityIssues(recentStats);
                
                var result = new DriftDetectionResult
                {
                    ReferenceDatasetId = referenceDatasetId,
                    Timestamp = DateTime.UtcNow,
                    OverallDriftScore = overallDriftScore,
                    FeatureDrift = featureDrift,
                    DistributionShift = distributionShift,
                    DataQualityIssues = qualityIssues,
                    ShouldRetrainModel = overallDriftScore > 0.3 || qualityIssues.Any(i => i.Severity == DataQualitySeverity.Critical)
                };
                
                _logger.LogInformation("Drift detection completed. Overall score: {Score}, Issues: {IssueCount}", 
                    overallDriftScore, qualityIssues.Count);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting data drift");
                throw;
            }
        }

        private async Task<DatasetStatistics> GetDatasetStatisticsAsync(string datasetId)
        {
            // In a real implementation, this would retrieve pre-calculated statistics or compute them
            return new DatasetStatistics
            {
                ExampleCount = 1000,
                AveragePromptLength = 50,
                TopicDistribution = new Dictionary<string, double>
                {
                    { "general", 0.4 },
                    { "technical", 0.3 },
                    { "creative", 0.2 },
                    { "business", 0.1 }
                }
            };
        }

        private DatasetStatistics CalculateStatisticsFromFeedback(List<Core.Models.FeedbackData> feedback)
        {
            // Calculate statistics from feedback data
            int totalPromptLength = feedback.Sum(f => f.OriginalPrompt?.Length ?? 0);
            double avgPromptLength = feedback.Count > 0 ? (double)totalPromptLength / feedback.Count : 0;
            
            // Simple topic distribution (in a real implementation, this would use proper topic modeling)
            var topicDist = new Dictionary<string, double>
            {
                { "general", 0.45 },
                { "technical", 0.25 },
                { "creative", 0.2 },
                { "business", 0.1 }
            };
            
            return new DatasetStatistics
            {
                ExampleCount = feedback.Count,
                AveragePromptLength = avgPromptLength,
                TopicDistribution = topicDist
            };
        }

        private Dictionary<string, double> CalculateFeatureDrift(
            DatasetStatistics referenceStats, 
            DatasetStatistics currentStats)
        {
            var drift = new Dictionary<string, double>();
            
            // Calculate normalized difference in prompt length
            double promptLengthRef = referenceStats.AveragePromptLength;
            double promptLengthCur = currentStats.AveragePromptLength;
            double promptLengthDrift = Math.Abs(promptLengthRef - promptLengthCur) / Math.Max(promptLengthRef, 1);
            drift["promptLength"] = Math.Min(promptLengthDrift, 1.0); // Cap at 1.0
            
            // Calculate example count difference
            double countDiff = Math.Abs(referenceStats.ExampleCount - currentStats.ExampleCount) / 
                               Math.Max(referenceStats.ExampleCount, 1);
            drift["exampleCount"] = Math.Min(countDiff, 1.0);
            
            return drift;
        }

        private double CalculateDistributionShift(
            Dictionary<string, double> referenceDistribution,
            Dictionary<string, double> currentDistribution)
        {
            double totalDifference = 0;
            int categoryCount = 0;
            
            // Calculate total difference across all categories
            var allCategories = new HashSet<string>(
                referenceDistribution.Keys.Concat(currentDistribution.Keys));
            
            foreach (var category in allCategories)
            {
                double refValue = referenceDistribution.GetValueOrDefault(category, 0);
                double curValue = currentDistribution.GetValueOrDefault(category, 0);
                
                totalDifference += Math.Abs(refValue - curValue);
                categoryCount++;
            }
            
            // Return average difference (normalized by category count)
            return categoryCount > 0 ? totalDifference / categoryCount : 0;
        }

        private List<DataQualityIssue> DetectQualityIssues(DatasetStatistics stats)
        {
            var issues = new List<DataQualityIssue>();
            
            // Check for low sample count
            if (stats.ExampleCount < 100)
            {
                issues.Add(new DataQualityIssue
                {
                    Type = "LowSampleCount",
                    Message = $"Only {stats.ExampleCount} examples available",
                    Severity = stats.ExampleCount < 10 
                        ? DataQualitySeverity.Critical 
                        : DataQualitySeverity.Warning
                });
            }
            
            // Check for very short prompts
            if (stats.AveragePromptLength < 10)
            {
                issues.Add(new DataQualityIssue
                {
                    Type = "ShortPrompts",
                    Message = $"Average prompt length is only {stats.AveragePromptLength:F1} characters",
                    Severity = DataQualitySeverity.Warning
                });
            }
            
            return issues;
        }
        
        private double CalculateOverallDriftScore(
            Dictionary<string, double> featureDrift,
            double distributionShift)
        {
            // Calculate weighted average of drift metrics
            double weightedSum = featureDrift.Values.Sum() + (distributionShift * 2); // Distribution shift has higher weight
            int totalWeight = featureDrift.Count + 2;
            
            double score = totalWeight > 0 ? weightedSum / totalWeight : 0;
            return Math.Min(score, 1.0); // Cap at 1.0
        }
    }

    public class DatasetStatistics
    {
        public int ExampleCount { get; set; }
        public double AveragePromptLength { get; set; }
        public Dictionary<string, double> TopicDistribution { get; set; } = new Dictionary<string, double>();
    }

    public class DriftDetectionResult
    {
        public string ReferenceDatasetId { get; set; }
        public DateTime Timestamp { get; set; }
        public double OverallDriftScore { get; set; }
        public Dictionary<string, double> FeatureDrift { get; set; } = new Dictionary<string, double>();
        public double DistributionShift { get; set; }
        public List<DataQualityIssue> DataQualityIssues { get; set; } = new List<DataQualityIssue>();
        public bool ShouldRetrainModel { get; set; }
    }

    public class DataQualityIssue
    {
        public string Type { get; set; }
        public string Message { get; set; }
        public DataQualitySeverity Severity { get; set; }
    }

    public enum DataQualitySeverity
    {
        Info,
        Warning,
        Critical
    }
}
