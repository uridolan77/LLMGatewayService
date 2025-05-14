using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLMGateway.Tuning.Core.Enums;
using LLMGateway.Tuning.Core.Interfaces;
using LLMGateway.Tuning.Core.Models;
using LLMGateway.Tuning.Data.Anonymization;
using Microsoft.Extensions.Logging;

namespace LLMGateway.Tuning.Data.Preparation
{
    public class DatasetGenerator
    {
        private readonly ILogger<DatasetGenerator> _logger;
        private readonly IFeedbackRepository _feedbackRepository;
        private readonly IDataAugmenter _dataAugmenter;
        private readonly IDatasetStorage _datasetStorage;
        private readonly DataAnonymizer _anonymizer;
        
        public DatasetGenerator(
            ILogger<DatasetGenerator> logger,
            IFeedbackRepository feedbackRepository,
            IDataAugmenter dataAugmenter,
            IDatasetStorage datasetStorage,
            DataAnonymizer anonymizer)
        {
            _logger = logger;
            _feedbackRepository = feedbackRepository;
            _dataAugmenter = dataAugmenter;
            _datasetStorage = datasetStorage;
            _anonymizer = anonymizer;
        }
        
        public async Task<string> GenerateTrainingDatasetAsync(DatasetGenerationOptions options)
        {
            _logger.LogInformation("Generating training dataset");
            
            // Collect feedback for dataset
            var feedbackRecords = await _feedbackRepository.GetFeedbackForDatasetGenerationAsync(
                options.Since,
                maxRecords: 1000,
                feedbackType: options.FeedbackType);
                
            _logger.LogInformation("Collected {Count} feedback records for dataset generation", 
                feedbackRecords.Count);
                
            // Transform feedback to training examples
            var trainingExamples = TransformFeedbackToTrainingExamples(feedbackRecords);
            
            // Anonymize data if requested
            if (options.AnonymizeData)
            {
                trainingExamples = AnonymizeTrainingExamples(trainingExamples);
                _logger.LogInformation("Anonymized training examples");
            }
            
            // Augment data if needed
            if (options.PerformAugmentation)
            {
                trainingExamples = await _dataAugmenter.AugmentExamplesAsync(trainingExamples);
                _logger.LogInformation("Augmented to {Count} training examples", trainingExamples.Count);
            }
            
            // Split data into training and validation sets
            (var trainingSet, var validationSet) = SplitDataset(trainingExamples, options.ValidationSplit);
            
            // Format for the specific model architecture
            var formattedTraining = FormatForModel(trainingSet, options.ModelType);
            var formattedValidation = FormatForModel(validationSet, options.ModelType);
            
            // Save datasets
            string datasetId = $"training_{DateTime.UtcNow:yyyyMMdd}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            await _datasetStorage.SaveDatasetAsync(datasetId, "training", formattedTraining);
            await _datasetStorage.SaveDatasetAsync(datasetId, "validation", formattedValidation);
            
            // Create dataset record
            var dataset = new Dataset
            {
                Id = datasetId,
                Name = options.DatasetName ?? $"Dataset-{DateTime.UtcNow:yyyyMMdd}",
                Description = options.Description ?? "Generated from user feedback",
                CreatedBy = options.CreatedBy,
                Type = options.DatasetType,
                Status = DatasetStatus.Ready,
                ExampleCount = trainingExamples.Count,
                Size = Encoding.UTF8.GetByteCount(formattedTraining) + Encoding.UTF8.GetByteCount(formattedValidation),
                Metadata = new Dictionary<string, string>
                {
                    { "SourceType", "Feedback" },
                    { "FeedbackTimespan", $"{options.Since:yyyy-MM-dd} to {DateTime.UtcNow:yyyy-MM-dd}" },
                    { "AnonymizedData", options.AnonymizeData.ToString() }
                },
                Splits = new List<DatasetSplit>
                {
                    new DatasetSplit
                    {
                        Name = "training",
                        StoragePath = $"{datasetId}/training",
                        ExampleCount = trainingSet.Count,
                        Size = Encoding.UTF8.GetByteCount(formattedTraining)
                    },
                    new DatasetSplit
                    {
                        Name = "validation",
                        StoragePath = $"{datasetId}/validation",
                        ExampleCount = validationSet.Count,
                        Size = Encoding.UTF8.GetByteCount(formattedValidation)
                    }
                }
            };
            
            _logger.LogInformation("Generated dataset {DatasetId} with {TrainingCount} training examples and {ValidationCount} validation examples",
                datasetId, trainingSet.Count, validationSet.Count);
            
            return datasetId;
        }
        
        private List<TrainingExample> TransformFeedbackToTrainingExamples(List<FeedbackData> feedbackRecords)
        {
            var examples = new List<TrainingExample>();
            
            foreach (var feedback in feedbackRecords)
            {
                // Only include feedback with useful corrections
                if (feedback.FeedbackType == FeedbackType.ManualCorrection && 
                    !string.IsNullOrWhiteSpace(feedback.CorrectedResponse))
                {
                    var example = new TrainingExample
                    {
                        SystemPrompt = "You are a helpful assistant.",
                        UserPrompt = feedback.OriginalPrompt,
                        ModelResponse = feedback.ModelResponse,
                        PreferredResponse = feedback.CorrectedResponse,
                        Tags = new List<string> { feedback.FeedbackType.ToString() }
                    };
                    
                    examples.Add(example);
                }
            }
            
            return examples;
        }
        
        private List<TrainingExample> AnonymizeTrainingExamples(List<TrainingExample> examples)
        {
            return examples.Select(example => {
                // Anonymize user prompts and preferred responses
                var anonymizedPrompt = _anonymizer.AnonymizeText(example.UserPrompt);
                var anonymizedResponse = _anonymizer.AnonymizeText(example.PreferredResponse);
                
                return example with
                {
                    UserPrompt = anonymizedPrompt.AnonymizedText,
                    PreferredResponse = anonymizedResponse.AnonymizedText,
                    Metadata = new Dictionary<string, string>(example.Metadata)
                    {
                        { "PromptAnonymizations", anonymizedPrompt.EntityReplacements.ToString() },
                        { "ResponseAnonymizations", anonymizedResponse.EntityReplacements.ToString() }
                    }
                };
            }).ToList();
        }
        
        private (List<TrainingExample>, List<TrainingExample>) SplitDataset(List<TrainingExample> examples, double validationSplit)
        {
            int validationCount = (int)(examples.Count * validationSplit);
            int trainingCount = examples.Count - validationCount;
            
            // Shuffle examples
            var random = new Random(42); // Fixed seed for reproducibility
            var shuffled = examples.OrderBy(x => random.Next()).ToList();
            
            return (
                shuffled.Take(trainingCount).ToList(),
                shuffled.Skip(trainingCount).Take(validationCount).ToList()
            );
        }
        
        private string FormatForModel(List<TrainingExample> examples, ModelType modelType)
        {
            // Format data based on the model type
            switch (modelType)
            {
                case ModelType.OpenAi:
                    return FormatForOpenAI(examples);
                case ModelType.Anthropic:
                    return FormatForAnthropic(examples);
                default:
                    return FormatForOpenAI(examples); // Default to OpenAI format
            }
        }
        
        private string FormatForOpenAI(List<TrainingExample> examples)
        {
            var formattedData = new System.Text.StringBuilder();
            
            foreach (var example in examples)
            {
                var jsonLine = System.Text.Json.JsonSerializer.Serialize(new
                {
                    messages = new[]
                    {
                        new { role = "system", content = example.SystemPrompt },
                        new { role = "user", content = example.UserPrompt },
                        new { role = "assistant", content = example.PreferredResponse }
                    }
                });
                
                formattedData.AppendLine(jsonLine);
            }
            
            return formattedData.ToString();
        }
        
        private string FormatForAnthropic(List<TrainingExample> examples)
        {
            var formattedData = new System.Text.StringBuilder();
            
            foreach (var example in examples)
            {
                formattedData.AppendLine($"Human: {example.SystemPrompt}");
                formattedData.AppendLine($"Human: {example.UserPrompt}");
                formattedData.AppendLine($"Assistant: {example.PreferredResponse}");
                formattedData.AppendLine();
            }
            
            return formattedData.ToString();
        }
    }

    public class DatasetGenerationOptions
    {
        public DateTime Since { get; set; } = DateTime.UtcNow.AddDays(-30);
        public string CreatedBy { get; set; }
        public string DatasetName { get; set; }
        public string Description { get; set; }
        public FeedbackType? FeedbackType { get; set; }
        public double ValidationSplit { get; set; } = 0.1;
        public bool AnonymizeData { get; set; } = true;
        public bool PerformAugmentation { get; set; } = false;
        public ModelType ModelType { get; set; } = ModelType.OpenAi;
        public DatasetType DatasetType { get; set; } = DatasetType.SupervisedFineTuning;
    }
}
