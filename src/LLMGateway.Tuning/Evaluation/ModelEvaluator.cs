using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using LLMGateway.Tuning.Core.Interfaces;
using LLMGateway.Tuning.Core.Models;
using LLMGateway.Tuning.Evaluation.Metrics;
using Microsoft.Extensions.Logging;

namespace LLMGateway.Tuning.Evaluation
{
    public class ModelEvaluator : IModelEvaluator
    {
        private readonly ILogger<ModelEvaluator> _logger;
        private readonly IModelAdapter _modelAdapter;
        private readonly IDatasetStorage _datasetStorage;
        private readonly MetricsCalculator _metricsCalculator;

        public ModelEvaluator(
            ILogger<ModelEvaluator> logger,
            IModelAdapter modelAdapter,
            IDatasetStorage datasetStorage,
            MetricsCalculator metricsCalculator)
        {
            _logger = logger;
            _modelAdapter = modelAdapter;
            _datasetStorage = datasetStorage;
            _metricsCalculator = metricsCalculator;
        }

        public async Task<Core.Interfaces.EvaluationResult> EvaluateModelAsync(Core.Interfaces.ModelEvaluationRequest request)
        {
            try
            {
                _logger.LogInformation("Evaluating model {ModelId} with dataset split {Split}", 
                    request.ModelId, request.DatasetSplit);

                // Load evaluation dataset
                var datasetContent = await _datasetStorage.LoadDatasetAsync(request.DatasetId, request.DatasetSplit);
                if (string.IsNullOrEmpty(datasetContent))
                {
                    _logger.LogError("Evaluation dataset not found: {DatasetId}/{Split}", 
                        request.DatasetId, request.DatasetSplit);
                    
                    return new EvaluationResult
                    {
                        ModelId = request.ModelId,
                        Success = false,
                        ErrorMessage = $"Evaluation dataset not found: {request.DatasetId}/{request.DatasetSplit}"
                    };
                }

                // Parse dataset into examples
                var examples = ParseDataset(datasetContent);
                _logger.LogInformation("Loaded {Count} examples for evaluation", examples.Count);                // Make predictions for each example
                var predictionResults = new List<Core.Interfaces.PredictionResult>();
                foreach (var example in examples)
                {
                    var prediction = await _modelAdapter.PredictAsync(request.ModelId, example.UserPrompt);
                    
                    var scores = _metricsCalculator.CalculateMetrics(
                        prediction, 
                        example.PreferredResponse,
                        request.Metrics);
                    
                    predictionResults.Add(new Core.Interfaces.PredictionResult
                    {
                        Id = example.Id,
                        Prediction = prediction,
                        Target = example.PreferredResponse,
                        Scores = scores
                    });
                }                // Calculate aggregate metrics
                var aggregateMetrics = _metricsCalculator.CalculateAggregateMetrics(predictionResults);
                
                _logger.LogInformation("Completed evaluation for model {ModelId} with {MetricsCount} metrics", 
                    request.ModelId, aggregateMetrics.Count);

                return new Core.Interfaces.EvaluationResult
                {
                    ModelId = request.ModelId,
                    DatasetId = request.DatasetId,
                    EvaluationTimestamp = DateTime.UtcNow,
                    ExamplesCount = examples.Count,
                    Metrics = aggregateMetrics,
                    DetailedResults = request.IncludeDetailedResults ? predictionResults : null,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating model {ModelId}", request.ModelId);
                return new Core.Interfaces.EvaluationResult
                {
                    ModelId = request.ModelId,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private List<TrainingExample> ParseDataset(string datasetContent)
        {
            // Parse dataset format based on content
            // This is a simplified implementation that assumes JSONL format
            var examples = new List<TrainingExample>();
            
            var lines = datasetContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                try
                {
                    var jsonDoc = System.Text.Json.JsonDocument.Parse(line);
                    var root = jsonDoc.RootElement;
                    
                    if (root.TryGetProperty("messages", out var messagesElement))
                    {
                        string systemPrompt = "";
                        string userPrompt = "";
                        string assistantResponse = "";
                        
                        foreach (var message in messagesElement.EnumerateArray())
                        {
                            var role = message.GetProperty("role").GetString();
                            var content = message.GetProperty("content").GetString();
                            
                            if (role == "system")
                                systemPrompt = content;
                            else if (role == "user")
                                userPrompt = content;
                            else if (role == "assistant")
                                assistantResponse = content;
                        }
                        
                        examples.Add(new TrainingExample
                        {
                            SystemPrompt = systemPrompt,
                            UserPrompt = userPrompt,
                            PreferredResponse = assistantResponse
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error parsing dataset line: {Line}", line.Substring(0, Math.Min(100, line.Length)));
                }
            }
            
            return examples;        }
    }
}
