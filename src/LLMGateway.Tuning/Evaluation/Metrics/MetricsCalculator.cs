using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace LLMGateway.Tuning.Evaluation.Metrics
{
    public class MetricsCalculator
    {
        private readonly ILogger<MetricsCalculator> _logger;
        
        public MetricsCalculator(ILogger<MetricsCalculator> logger)
        {
            _logger = logger;
        }
        
        public Dictionary<string, double> CalculateMetrics(
            string prediction, 
            string target, 
            List<string> metrics)
        {
            var results = new Dictionary<string, double>();
            
            foreach (var metric in metrics)
            {
                switch (metric.ToLower())
                {
                    case "exactmatch":
                        results[metric] = CalculateExactMatch(prediction, target);
                        break;
                    case "bleu":
                        results[metric] = CalculateBleuScore(prediction, target);
                        break;
                    case "rouge":
                        results[metric] = CalculateRougeScore(prediction, target);
                        break;
                    case "f1":
                        results[metric] = CalculateF1Score(prediction, target);
                        break;
                    case "cosinesimilarity":
                        results[metric] = CalculateCosineSimilarity(prediction, target);
                        break;
                    default:
                        _logger.LogWarning("Unsupported metric: {Metric}", metric);
                        break;
                }
            }
            
            return results;
        }
          public Dictionary<string, double> CalculateAggregateMetrics(List<Core.Interfaces.PredictionResult> results)
        {
            if (results == null || results.Count == 0)
                return new Dictionary<string, double>();
                
            var aggregateMetrics = new Dictionary<string, double>();
            
            // Get all metrics from the first result
            var sampleMetrics = results.First().Scores.Keys;
            
            foreach (var metric in sampleMetrics)
            {
                // Calculate average for each metric
                var average = results.Average(r => r.Scores.ContainsKey(metric) ? r.Scores[metric] : 0);
                aggregateMetrics[metric] = average;
            }
            
            return aggregateMetrics;
        }
        
        private double CalculateExactMatch(string prediction, string target)
        {
            return string.Equals(prediction.Trim(), target.Trim(), StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0;
        }
        
        private double CalculateBleuScore(string prediction, string target)
        {
            // Simple BLEU-like implementation (this is a simplified version)
            var predictionTokens = Tokenize(prediction);
            var targetTokens = Tokenize(target);
            
            // Count matching tokens (exact match)
            int matches = 0;
            foreach (var token in predictionTokens)
            {
                if (targetTokens.Contains(token))
                {
                    matches++;
                    // Remove the matched token to avoid double counting
                    targetTokens.Remove(token);
                }
            }
            
            // Calculate precision
            double precision = predictionTokens.Count > 0 ? (double)matches / predictionTokens.Count : 0;
            
            // Apply brevity penalty
            double brevityPenalty = 1.0;
            if (predictionTokens.Count < targetTokens.Count)
            {
                brevityPenalty = Math.Exp(1 - (double)targetTokens.Count / predictionTokens.Count);
            }
            
            return precision * brevityPenalty;
        }
        
        private double CalculateRougeScore(string prediction, string target)
        {
            // Simple ROUGE-like implementation (ROUGE-1)
            var predictionTokens = Tokenize(prediction);
            var targetTokens = Tokenize(target);
            
            var targetTokenSet = new HashSet<string>(targetTokens);
            
            // Count overlapping tokens
            int overlapping = 0;
            foreach (var token in predictionTokens)
            {
                if (targetTokenSet.Contains(token))
                {
                    overlapping++;
                }
            }
            
            // Calculate recall and precision
            double recall = targetTokens.Count > 0 ? (double)overlapping / targetTokens.Count : 0;
            double precision = predictionTokens.Count > 0 ? (double)overlapping / predictionTokens.Count : 0;
            
            // Calculate F1 score (ROUGE-F)
            if (recall + precision == 0) return 0;
            return 2 * recall * precision / (recall + precision);
        }
        
        private double CalculateF1Score(string prediction, string target)
        {
            var predictionTokens = Tokenize(prediction);
            var targetTokens = Tokenize(target);
            
            var predictionSet = new HashSet<string>(predictionTokens);
            var targetSet = new HashSet<string>(targetTokens);
            
            var intersection = predictionSet.Intersect(targetSet).Count();
            
            if (predictionSet.Count == 0 || targetSet.Count == 0)
                return 0;
            
            double precision = (double)intersection / predictionSet.Count;
            double recall = (double)intersection / targetSet.Count;
            
            if (precision + recall == 0)
                return 0;
            
            return 2 * precision * recall / (precision + recall);
        }
        
        private double CalculateCosineSimilarity(string prediction, string target)
        {
            // Simple cosine similarity based on term frequency
            var predictionTokens = Tokenize(prediction);
            var targetTokens = Tokenize(target);
            
            var allTokens = new HashSet<string>(predictionTokens.Concat(targetTokens));
            
            // Create term frequency vectors
            var predictionVector = CreateTermFrequencyVector(predictionTokens, allTokens);
            var targetVector = CreateTermFrequencyVector(targetTokens, allTokens);
            
            // Calculate cosine similarity
            double dotProduct = 0;
            double predictionMagnitude = 0;
            double targetMagnitude = 0;
            
            for (int i = 0; i < predictionVector.Length; i++)
            {
                dotProduct += predictionVector[i] * targetVector[i];
                predictionMagnitude += predictionVector[i] * predictionVector[i];
                targetMagnitude += targetVector[i] * targetVector[i];
            }
            
            predictionMagnitude = Math.Sqrt(predictionMagnitude);
            targetMagnitude = Math.Sqrt(targetMagnitude);
            
            if (predictionMagnitude == 0 || targetMagnitude == 0)
                return 0;
            
            return dotProduct / (predictionMagnitude * targetMagnitude);
        }
        
        private List<string> Tokenize(string text)
        {
            // Simple tokenization by splitting on whitespace and removing punctuation
            return Regex.Replace(text.ToLower(), @"[^\w\s]", "")
                .Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();
        }
        
        private double[] CreateTermFrequencyVector(List<string> tokens, HashSet<string> vocabulary)
        {
            var vector = new double[vocabulary.Count];
            var vocabularyList = vocabulary.ToList();
            
            foreach (var token in tokens)
            {
                int index = vocabularyList.IndexOf(token);
                if (index >= 0)
                {
                    vector[index]++;
                }
            }
              return vector;
        }
    }
}
