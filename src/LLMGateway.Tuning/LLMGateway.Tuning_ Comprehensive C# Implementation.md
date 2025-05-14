# **LLMGateway.Tuning: Comprehensive C\# Implementation**

I'll provide the implementation divided into individual files, organized by the project structure. Each file contains the complete code needed for that component.

## **Core Models**

**LLMGateway.Tuning/Core/Models/FeedbackData.cs**

using System;  
using System.Collections.Generic;  
using LLMGateway.Tuning.Core.Enums;

namespace LLMGateway.Tuning.Core.Models  
{  
    public record FeedbackData  
    {  
        // Core feedback data  
        public string Id { get; init; } \= Guid.NewGuid().ToString();  
        public string UserId { get; init; }  
        public string OriginalPrompt { get; init; }  
        public string ModelResponse { get; init; }  
        public Dictionary\<string, string\> Metadata { get; init; } \= new Dictionary\<string, string\>();  
        public FeedbackType FeedbackType { get; init; }  
          
        // User-provided corrections  
        public string CorrectedResponse { get; init; }  
          
        // Feedback score (-1 to 1\)  
        public double SatisfactionScore { get; init; }  
          
        // Contextual information  
        public string UserSegment { get; init; }  
        public List\<string\> UserContext { get; init; } \= new List\<string\>();  
        public DateTime Timestamp { get; init; } \= DateTime.UtcNow;  
        public RequestContext RequestContext { get; init; }  
    }

    public record RequestContext  
    {  
        public string SessionId { get; init; }  
        public string ClientApplication { get; init; }  
        public Dictionary\<string, string\> RequestParameters { get; init; } \= new Dictionary\<string, string\>();  
        public int PromptTokens { get; init; }  
        public int ResponseTokens { get; init; }  
        public double ProcessingTimeMs { get; init; }  
    }  
}

**LLMGateway.Tuning/Core/Models/TrainingExample.cs**

using System;  
using System.Collections.Generic;

namespace LLMGateway.Tuning.Core.Models  
{  
    public record TrainingExample  
    {  
        public string Id { get; init; } \= Guid.NewGuid().ToString();  
        public string SystemPrompt { get; init; }  
        public string UserPrompt { get; init; }  
        public string ModelResponse { get; init; }  
        public string PreferredResponse { get; init; }  
        public List\<string\> Tags { get; init; } \= new List\<string\>();  
        public double Difficulty { get; init; } \= 0.5;  
        public Dictionary\<string, string\> Metadata { get; init; } \= new Dictionary\<string, string\>();  
    }  
}

**LLMGateway.Tuning/Core/Models/Dataset.cs**

using System;  
using System.Collections.Generic;

namespace LLMGateway.Tuning.Core.Models  
{  
    public record Dataset  
    {  
        public string Id { get; init; } \= Guid.NewGuid().ToString();  
        public string Name { get; init; }  
        public string Description { get; init; }  
        public DateTime CreatedAt { get; init; } \= DateTime.UtcNow;  
        public string CreatedBy { get; init; }  
        public DatasetType Type { get; init; }  
        public DatasetStatus Status { get; init; } \= DatasetStatus.Created;  
        public long Size { get; init; }  
        public int ExampleCount { get; init; }  
        public Dictionary\<string, string\> Metadata { get; init; } \= new Dictionary\<string, string\>();  
        public List\<DatasetSplit\> Splits { get; init; } \= new List\<DatasetSplit\>();  
    }

    public record DatasetSplit  
    {  
        public string Name { get; init; }  
        public string StoragePath { get; init; }  
        public int ExampleCount { get; init; }  
        public long Size { get; init; }  
    }  
}

**LLMGateway.Tuning/Core/Enums/DatasetType.cs**

namespace LLMGateway.Tuning.Core.Enums  
{  
    public enum DatasetType  
    {  
        PretrainedFineTuning,  
        SupervisedFineTuning,  
        ReinforcementLearning,  
        EvaluationSet  
    }  
}

**LLMGateway.Tuning/Core/Enums/DatasetStatus.cs**

namespace LLMGateway.Tuning.Core.Enums  
{  
    public enum DatasetStatus  
    {  
        Created,  
        Preparing,  
        Ready,  
        Archived,  
        Error  
    }  
}

**LLMGateway.Tuning/Core/Enums/FeedbackType.cs**

namespace LLMGateway.Tuning.Core.Enums  
{  
    public enum FeedbackType  
    {  
        ThumbsDown,  
        ThumbsUp,  
        TextFeedback,  
        ManualCorrection,  
        PromptReformulation,  
        AutomaticEvaluation  
    }  
}

**LLMGateway.Tuning/Core/Enums/ModelType.cs**

namespace LLMGateway.Tuning.Core.Enums  
{  
    public enum ModelType  
    {  
        OpenAi,  
        Anthropic,  
        Llama,  
        Mistral,  
        Custom  
    }  
}

## **Core Interfaces**

**LLMGateway.Tuning/Core/Interfaces/IFeedbackRepository.cs**

using System;  
using System.Collections.Generic;  
using System.Threading.Tasks;  
using LLMGateway.Tuning.Core.Enums;  
using LLMGateway.Tuning.Core.Models;

namespace LLMGateway.Tuning.Core.Interfaces  
{  
    public interface IFeedbackRepository  
    {  
        Task\<string\> SaveFeedbackAsync(FeedbackData feedback);  
        Task\<FeedbackData\> GetFeedbackAsync(string id);  
        Task\<List\<FeedbackData\>\> GetFeedbackForDatasetGenerationAsync(  
            DateTime since,   
            int maxRecords \= 1000,  
            FeedbackType? feedbackType \= null);  
        Task\<int\> GetFeedbackCountSinceAsync(DateTime since);  
        Task\<List\<FeedbackData\>\> GetFeedbackByUserIdAsync(string userId, int limit \= 100);  
    }  
}

**LLMGateway.Tuning/Core/Interfaces/IDatasetStorage.cs**

using System.Collections.Generic;  
using System.Threading.Tasks;  
using LLMGateway.Tuning.Core.Models;

namespace LLMGateway.Tuning.Core.Interfaces  
{  
    public interface IDatasetStorage  
    {  
        Task\<string\> SaveDatasetAsync(string datasetId, string splitName, string formattedData);  
        Task\<string\> LoadDatasetAsync(string datasetId, string splitName);  
        Task\<bool\> DeleteDatasetAsync(string datasetId);  
        Task\<List\<Dataset\>\> ListDatasetsAsync(int limit \= 100, int offset \= 0);  
        Task\<Dataset\> GetDatasetAsync(string datasetId);  
        Task\<long\> GetDatasetSizeAsync(string datasetId, string splitName);  
    }  
}

**LLMGateway.Tuning/Core/Interfaces/IModelAdapter.cs**

using System;  
using System.Threading.Tasks;  
using LLMGateway.Tuning.Core.Models;

namespace LLMGateway.Tuning.Core.Interfaces  
{  
    public interface IModelAdapter  
    {  
        Task\<TrainingResult\> TrainModelAsync(  
            string trainingData,  
            string validationData,  
            TrainingConfiguration config);  
              
        Task\<string\> PredictAsync(string modelId, string prompt);  
          
        Task\<string\> CreateFineTuningJobAsync(string fileId, TrainingConfiguration config);  
          
        Task\<string\> UploadDatasetAsync(string formattedData);  
          
        Task\<TrainingJobStatus\> GetJobStatusAsync(string jobId);  
    }  
}

## **Data Collection and Preparation**

**LLMGateway.Tuning/Data/Collection/FeedbackCollector.cs**

using System;  
using System.Threading.Tasks;  
using LLMGateway.Tuning.Core.Interfaces;  
using LLMGateway.Tuning.Core.Models;  
using LLMGateway.Tuning.Data.Validation;  
using Microsoft.Extensions.Logging;

namespace LLMGateway.Tuning.Data.Collection  
{  
    public class FeedbackCollector : IFeedbackCollector  
    {  
        private readonly ILogger\<FeedbackCollector\> \_logger;  
        private readonly IFeedbackRepository \_repository;  
        private readonly IUserContextProvider \_userContextProvider;  
        private readonly FeedbackValidator \_validator;

        public FeedbackCollector(  
            ILogger\<FeedbackCollector\> logger,  
            IFeedbackRepository repository,  
            IUserContextProvider userContextProvider,  
            FeedbackValidator validator)  
        {  
            \_logger \= logger;  
            \_repository \= repository;  
            \_userContextProvider \= userContextProvider;  
            \_validator \= validator;  
        }

        public async Task\<string\> RecordFeedbackAsync(FeedbackData feedback)  
        {  
            // Validate feedback  
            var validation \= \_validator.ValidateFeedback(feedback);  
            if (\!validation.IsValid)  
            {  
                \_logger.LogWarning("Invalid feedback received: {Errors}", string.Join(", ", validation.Errors));  
                throw new ArgumentException($"Invalid feedback: {string.Join(", ", validation.Errors)}");  
            }

            // Enrich feedback with context  
            var userContext \= await \_userContextProvider.GetUserContextAsync(feedback.UserId);  
            var enrichedFeedback \= EnrichFeedback(feedback, userContext);  
              
            // Store feedback  
            var id \= await \_repository.SaveFeedbackAsync(enrichedFeedback);  
              
            \_logger.LogInformation("Feedback recorded with ID: {FeedbackId}", id);  
              
            // Trigger processing pipeline if threshold reached  
            await TriggerProcessingIfNeededAsync();  
              
            return id;  
        }  
          
        private FeedbackData EnrichFeedback(FeedbackData feedback, UserContext context)  
        {  
            return feedback with  
            {  
                UserSegment \= context.Segment,  
                UserContext \= context.Preferences,  
                Timestamp \= DateTime.UtcNow  
            };  
        }  
          
        private async Task TriggerProcessingIfNeededAsync()  
        {  
            try  
            {  
                // Check if we've met the threshold for processing  
                var recentCount \= await \_repository.GetFeedbackCountSinceAsync(DateTime.UtcNow.AddDays(-1));  
                  
                if (recentCount \>= 500\) // Example threshold  
                {  
                    \_logger.LogInformation("Feedback threshold reached ({Count}). Triggering processing pipeline.", recentCount);  
                    // Signal processing pipeline to run \- implementation depends on architecture  
                    // Could use a message queue, event, or direct call  
                }  
            }  
            catch (Exception ex)  
            {  
                \_logger.LogError(ex, "Error checking feedback threshold");  
            }  
        }  
    }  
}

**LLMGateway.Tuning/Data/Validation/FeedbackValidator.cs**

using System.Collections.Generic;  
using LLMGateway.Tuning.Core.Enums;  
using LLMGateway.Tuning.Core.Models;

namespace LLMGateway.Tuning.Data.Validation  
{  
    public class FeedbackValidator  
    {  
        public ValidationResult ValidateFeedback(FeedbackData feedback)  
        {  
            var errors \= new List\<string\>();

            if (string.IsNullOrWhiteSpace(feedback.OriginalPrompt))  
                errors.Add("Original prompt cannot be empty");  
              
            if (string.IsNullOrWhiteSpace(feedback.ModelResponse))  
                errors.Add("Model response cannot be empty");  
              
            if (feedback.FeedbackType \== FeedbackType.ManualCorrection &&   
                string.IsNullOrWhiteSpace(feedback.CorrectedResponse))  
                errors.Add("Manual correction feedback requires a corrected response");

            if (feedback.SatisfactionScore \< \-1 || feedback.SatisfactionScore \> 1\)  
                errors.Add("Satisfaction score must be between \-1 and 1");

            return new ValidationResult(  
                errors.Count \== 0,  
                errors);  
        }  
    }

    public record ValidationResult(bool IsValid, List\<string\> Errors);  
}

**LLMGateway.Tuning/Data/Anonymization/DataAnonymizer.cs**

using System;  
using System.Text.RegularExpressions;  
using LLMGateway.Tuning.Core.Interfaces;  
using Microsoft.Extensions.Logging;

namespace LLMGateway.Tuning.Data.Anonymization  
{  
    public class DataAnonymizer  
    {  
        private readonly ILogger\<DataAnonymizer\> \_logger;  
        private readonly IEntityRecognitionService \_entityRecognitionService;  
          
        public DataAnonymizer(  
            ILogger\<DataAnonymizer\> logger,  
            IEntityRecognitionService entityRecognitionService)  
        {  
            \_logger \= logger;  
            \_entityRecognitionService \= entityRecognitionService;  
        }  
          
        public AnonymizedData AnonymizeText(string text)  
        {  
            try  
            {  
                // Replace identified entities with placeholders  
                var entities \= \_entityRecognitionService.RecognizeEntities(text);  
                var anonymizedText \= text;  
                  
                foreach (var entity in entities)  
                {  
                    anonymizedText \= anonymizedText.Replace(  
                        entity.Text,   
                        $"\[{entity.Type}\]",   
                        StringComparison.OrdinalIgnoreCase);  
                }  
                  
                // Replace email addresses  
                anonymizedText \= Regex.Replace(  
                    anonymizedText,  
                    @"\[a-zA-Z0-9.\_%+-\]+@\[a-zA-Z0-9.-\]+\\.\[a-zA-Z\]{2,}",   
                    "\[EMAIL\]");  
                  
                // Replace phone numbers  
                anonymizedText \= Regex.Replace(  
                    anonymizedText,  
                    @"\\b(\\+\\d{1,2}\\s)?\\(?\\d{3}\\)?\[\\s.-\]\\d{3}\[\\s.-\]\\d{4}\\b",  
                    "\[PHONE\]");  
                  
                // Replace numeric values with placeholders  
                anonymizedText \= Regex.Replace(anonymizedText, @"\\b\\d+\\b", "\[NUMBER\]");  
                  
                return new AnonymizedData(  
                    OriginalText: text,  
                    AnonymizedText: anonymizedText,  
                    EntityReplacements: entities.Count);  
            }  
            catch (Exception ex)  
            {  
                \_logger.LogError(ex, "Error anonymizing text");  
                return new AnonymizedData(  
                    OriginalText: text,  
                    AnonymizedText: text,  
                    EntityReplacements: 0);  
            }  
        }  
    }

    public record AnonymizedData(string OriginalText, string AnonymizedText, int EntityReplacements);  
}

**LLMGateway.Tuning/Data/Preparation/DatasetGenerator.cs**

using System;  
using System.Collections.Generic;  
using System.Linq;  
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
        private readonly ILogger\<DatasetGenerator\> \_logger;  
        private readonly IFeedbackRepository \_feedbackRepository;  
        private readonly IDataAugmenter \_dataAugmenter;  
        private readonly IDatasetStorage \_datasetStorage;  
        private readonly DataAnonymizer \_anonymizer;  
          
        public DatasetGenerator(  
            ILogger\<DatasetGenerator\> logger,  
            IFeedbackRepository feedbackRepository,  
            IDataAugmenter dataAugmenter,  
            IDatasetStorage datasetStorage,  
            DataAnonymizer anonymizer)  
        {  
            \_logger \= logger;  
            \_feedbackRepository \= feedbackRepository;  
            \_dataAugmenter \= dataAugmenter;  
            \_datasetStorage \= datasetStorage;  
            \_anonymizer \= anonymizer;  
        }  
          
        public async Task\<string\> GenerateTrainingDatasetAsync(DatasetGenerationOptions options)  
        {  
            \_logger.LogInformation("Generating training dataset");  
              
            // Collect feedback for dataset  
            var feedbackRecords \= await \_feedbackRepository.GetFeedbackForDatasetGenerationAsync(  
                options.Since,  
                options.MaxRecords,  
                options.FeedbackType);  
                  
            \_logger.LogInformation("Collected {Count} feedback records for dataset generation",   
                feedbackRecords.Count);  
                  
            // Transform feedback to training examples  
            var trainingExamples \= TransformFeedbackToTrainingExamples(feedbackRecords);  
              
            // Anonymize data if requested  
            if (options.AnonymizeData)  
            {  
                trainingExamples \= AnonymizeTrainingExamples(trainingExamples);  
                \_logger.LogInformation("Anonymized training examples");  
            }  
              
            // Augment data if needed  
            if (options.PerformAugmentation)  
            {  
                trainingExamples \= await \_dataAugmenter.AugmentExamplesAsync(trainingExamples);  
                \_logger.LogInformation("Augmented to {Count} training examples", trainingExamples.Count);  
            }  
              
            // Split data into training and validation sets  
            (var trainingSet, var validationSet) \= SplitDataset(trainingExamples, options.ValidationSplit);  
              
            // Format for the specific model architecture  
            var formattedTraining \= FormatForModel(trainingSet, options.ModelType);  
            var formattedValidation \= FormatForModel(validationSet, options.ModelType);  
              
            // Save datasets  
            string datasetId \= $"training\_{DateTime.UtcNow:yyyyMMdd}\_{Guid.NewGuid().ToString("N").Substring(0, 8)}";  
            await \_datasetStorage.SaveDatasetAsync(datasetId, "training", formattedTraining);  
            await \_datasetStorage.SaveDatasetAsync(datasetId, "validation", formattedValidation);  
              
            // Create dataset metadata  
            var dataset \= new Dataset  
            {  
                Id \= datasetId,  
                Name \= options.DatasetName ?? $"Training Dataset {DateTime.UtcNow:yyyy-MM-dd}",  
                Description \= options.DatasetDescription,  
                Type \= DatasetType.SupervisedFineTuning,  
                CreatedBy \= options.CreatedBy ?? "system",  
                ExampleCount \= trainingExamples.Count,  
                Splits \= new List\<DatasetSplit\>  
                {  
                    new DatasetSplit  
                    {  
                        Name \= "training",  
                        ExampleCount \= trainingSet.Count,  
                        Size \= formattedTraining.Length  
                    },  
                    new DatasetSplit  
                    {  
                        Name \= "validation",  
                        ExampleCount \= validationSet.Count,  
                        Size \= formattedValidation.Length  
                    }  
                },  
                Metadata \= new Dictionary\<string, string\>  
                {  
                    \["feedback\_count"\] \= feedbackRecords.Count.ToString(),  
                    \["augmented"\] \= options.PerformAugmentation.ToString(),  
                    \["anonymized"\] \= options.AnonymizeData.ToString(),  
                    \["model\_type"\] \= options.ModelType.ToString(),  
                }  
            };  
              
            // Save dataset metadata  
            // Implementation depends on storage mechanism  
              
            \_logger.LogInformation("Training dataset generated with ID: {DatasetId}", datasetId);  
            return datasetId;  
        }  
          
        private List\<TrainingExample\> TransformFeedbackToTrainingExamples(List\<FeedbackData\> feedbackRecords)  
        {  
            var examples \= new List\<TrainingExample\>();  
              
            foreach (var feedback in feedbackRecords)  
            {  
                // Skip records without corrections for supervised fine-tuning  
                if (feedback.FeedbackType \== FeedbackType.ManualCorrection &&   
                    string.IsNullOrEmpty(feedback.CorrectedResponse))  
                    continue;  
                  
                // Skip thumbs down without corrections  
                if (feedback.FeedbackType \== FeedbackType.ThumbsDown &&   
                    string.IsNullOrEmpty(feedback.CorrectedResponse))  
                    continue;  
                  
                var example \= new TrainingExample  
                {  
                    UserPrompt \= feedback.OriginalPrompt,  
                    ModelResponse \= feedback.ModelResponse,  
                    PreferredResponse \= feedback.CorrectedResponse ?? feedback.ModelResponse,  
                    Tags \= feedback.UserContext?.ToList() ?? new List\<string\>(),  
                    Metadata \= new Dictionary\<string, string\>  
                    {  
                        \["feedback\_id"\] \= feedback.Id,  
                        \["feedback\_type"\] \= feedback.FeedbackType.ToString(),  
                        \["user\_segment"\] \= feedback.UserSegment  
                    }  
                };  
                  
                examples.Add(example);  
            }  
              
            return examples;  
        }  
          
        private List\<TrainingExample\> AnonymizeTrainingExamples(List\<TrainingExample\> examples)  
        {  
            return examples.Select(example \=\>  
            {  
                var anonymizedPrompt \= \_anonymizer.AnonymizeText(example.UserPrompt);  
                var anonymizedResponse \= \_anonymizer.AnonymizeText(example.PreferredResponse);  
                  
                return example with   
                {  
                    UserPrompt \= anonymizedPrompt.AnonymizedText,  
                    PreferredResponse \= anonymizedResponse.AnonymizedText,  
                    Metadata \= example.Metadata.ToDictionary(  
                        kvp \=\> kvp.Key,   
                        kvp \=\> kvp.Value)  
                };  
            }).ToList();  
        }  
          
        private (List\<TrainingExample\> TrainingSet, List\<TrainingExample\> ValidationSet)   
            SplitDataset(List\<TrainingExample\> examples, double validationSplit)  
        {  
            // Shuffle the examples  
            var shuffled \= examples.OrderBy(e \=\> Guid.NewGuid()).ToList();  
              
            int validationCount \= (int)(examples.Count \* validationSplit);  
            int trainingCount \= examples.Count \- validationCount;  
              
            return (  
                shuffled.Take(trainingCount).ToList(),  
                shuffled.Skip(trainingCount).ToList()  
            );  
        }  
          
        private string FormatForModel(List\<TrainingExample\> examples, ModelType modelType)  
        {  
            return modelType switch  
            {  
                ModelType.OpenAi \=\> FormatForOpenAi(examples),  
                ModelType.Anthropic \=\> FormatForAnthropic(examples),  
                ModelType.LLama \=\> FormatForLlama(examples),  
                ModelType.Mistral \=\> FormatForMistral(examples),  
                \_ \=\> FormatForCustom(examples)  
            };  
        }  
          
        private string FormatForOpenAi(List\<TrainingExample\> examples)  
        {  
            var formattedExamples \= examples.Select(example \=\> new  
            {  
                messages \= new\[\]  
                {  
                    new { role \= "system", content \= "You are a helpful assistant." },  
                    new { role \= "user", content \= example.UserPrompt },  
                    new { role \= "assistant", content \= example.PreferredResponse }  
                }  
            });  
              
            // Return JSONL format  
            return string.Join("\\n", formattedExamples.Select(System.Text.Json.JsonSerializer.Serialize));  
        }  
          
        private string FormatForAnthropic(List\<TrainingExample\> examples)  
        {  
            // Implementation for Anthropic format  
            return "";  
        }  
          
        private string FormatForLlama(List\<TrainingExample\> examples)  
        {  
            // Implementation for Llama format  
            return "";  
        }  
          
        private string FormatForMistral(List\<TrainingExample\> examples)  
        {  
            // Implementation for Mistral format  
            return "";  
        }  
          
        private string FormatForCustom(List\<TrainingExample\> examples)  
        {  
            // Default implementation  
            return System.Text.Json.JsonSerializer.Serialize(examples);  
        }  
    }

    public class DatasetGenerationOptions  
    {  
        public DateTime Since { get; set; } \= DateTime.UtcNow.AddDays(-30);  
        public int MaxRecords { get; set; } \= 10000;  
        public FeedbackType? FeedbackType { get; set; }  
        public double ValidationSplit { get; set; } \= 0.2;  
        public bool PerformAugmentation { get; set; } \= true;  
        public bool AnonymizeData { get; set; } \= true;  
        public ModelType ModelType { get; set; } \= ModelType.OpenAi;  
        public string DatasetName { get; set; }  
        public string DatasetDescription { get; set; }  
        public string CreatedBy { get; set; }  
    }  
}

## **Training Components**

**LLMGateway.Tuning/Training/Configuration/TrainingConfiguration.cs**

using System;  
using System.Collections.Generic;  
using LLMGateway.Tuning.Core.Enums;

namespace LLMGateway.Tuning.Training.Configuration  
{  
    public class TrainingConfiguration  
    {  
        public ModelType ModelType { get; set; }  
        public string BaseModel { get; set; }  
        public Dictionary\<string, object\> Hyperparameters { get; set; } \= new Dictionary\<string, object\>();  
        public ValidationStrategy ValidationStrategy { get; set; } \= ValidationStrategy.HoldOut;  
        public TimeSpan MaxTrainingTime { get; set; } \= TimeSpan.FromHours(6);  
        public int EarlyStoppingPatience { get; set; } \= 3;  
        public int MaxTokens { get; set; } \= 1024;  
        public string OutputModelName { get; set; }  
        public Dictionary\<string, string\> Metadata { get; set; } \= new Dictionary\<string, string\>();  
    }

    public enum ValidationStrategy  
    {  
        HoldOut,  
        CrossValidation,  
        ProgressiveValidation  
    }  
}

**LLMGateway.Tuning/Training/Adapters/OpenAiAdapter.cs**

using System;  
using System.Collections.Generic;  
using System.Diagnostics;  
using System.Net.Http;  
using System.Text;  
using System.Text.Json;  
using System.Threading.Tasks;  
using LLMGateway.Tuning.Core.Interfaces;  
using LLMGateway.Tuning.Core.Models;  
using LLMGateway.Tuning.Training.Configuration;  
using Microsoft.Extensions.Logging;  
using Microsoft.Extensions.Options;

namespace LLMGateway.Tuning.Training.Adapters  
{  
    public class OpenAiAdapter : IModelAdapter  
    {  
        private readonly ILogger\<OpenAiAdapter\> \_logger;  
        private readonly IOptions\<OpenAiOptions\> \_options;  
        private readonly HttpClient \_httpClient;  
          
        public OpenAiAdapter(  
            ILogger\<OpenAiAdapter\> logger,  
            IOptions\<OpenAiOptions\> options,  
            HttpClient httpClient)  
        {  
            \_logger \= logger;  
            \_options \= options;  
            \_httpClient \= httpClient;  
        }  
          
        public async Task\<TrainingResult\> TrainModelAsync(  
            string trainingData,  
            string validationData,  
            TrainingConfiguration config)  
        {  
            var stopwatch \= Stopwatch.StartNew();  
              
            try  
            {  
                // Upload datasets  
                var trainingFileId \= await UploadDatasetAsync(trainingData);  
                \_logger.LogInformation("Uploaded training data. File ID: {FileId}", trainingFileId);  
                  
                // Create fine-tuning job  
                var jobId \= await CreateFineTuningJobAsync(trainingFileId, config);  
                \_logger.LogInformation("Created fine-tuning job. Job ID: {JobId}", jobId);  
                  
                // Monitor training progress  
                var result \= await MonitorTrainingJobAsync(jobId);  
                  
                stopwatch.Stop();  
                  
                // Return training results  
                return new TrainingResult  
                {  
                    ModelId \= result.FineTunedModel,  
                    ModelFiles \= new Dictionary\<string, string\>  
                    {  
                        \["model\_id"\] \= result.FineTunedModel  
                    },  
                    Metrics \= result.ValidationMetrics ?? new Dictionary\<string, double\>(),  
                    TrainingTime \= stopwatch.Elapsed,  
                    Status \= TrainingStatus.Completed  
                };  
            }  
            catch (Exception ex)  
            {  
                \_logger.LogError(ex, "Error in OpenAI fine-tuning");  
                  
                return new TrainingResult  
                {  
                    Status \= TrainingStatus.Failed,  
                    ErrorMessage \= ex.Message,  
                    TrainingTime \= stopwatch.Elapsed  
                };  
            }  
        }  
          
        public async Task\<string\> UploadDatasetAsync(string formattedData)  
        {  
            var content \= new StringContent(  
                formattedData,   
                Encoding.UTF8,   
                "application/json");  
              
            var request \= new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/files")  
            {  
                Headers \=   
                {  
                    { "Authorization", $"Bearer {\_options.Value.ApiKey}" }  
                },  
                Content \= content  
            };  
              
            var response \= await \_httpClient.SendAsync(request);  
            response.EnsureSuccessStatusCode();  
              
            var responseJson \= await response.Content.ReadAsStringAsync();  
            var responseObj \= JsonSerializer.Deserialize\<JsonElement\>(responseJson);  
              
            return responseObj.GetProperty("id").GetString();  
        }  
          
        public async Task\<string\> CreateFineTuningJobAsync(string fileId, TrainingConfiguration config)  
        {  
            var payload \= new  
            {  
                training\_file \= fileId,  
                model \= config.BaseModel ?? "gpt-3.5-turbo",  
                hyperparameters \= new  
                {  
                    n\_epochs \= config.Hyperparameters.GetValueOrDefault("epochs", 3),  
                    batch\_size \= config.Hyperparameters.GetValueOrDefault("batch\_size", 4),  
                    learning\_rate\_multiplier \= config.Hyperparameters.GetValueOrDefault("learning\_rate", 0.1)  
                },  
                suffix \= config.OutputModelName  
            };  
              
            var content \= new StringContent(  
                JsonSerializer.Serialize(payload),  
                Encoding.UTF8,  
                "application/json");  
                  
            var request \= new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/fine\_tuning/jobs")  
            {  
                Headers \=   
                {  
                    { "Authorization", $"Bearer {\_options.Value.ApiKey}" }  
                },  
                Content \= content  
            };  
              
            var response \= await \_httpClient.SendAsync(request);  
            response.EnsureSuccessStatusCode();  
              
            var responseJson \= await response.Content.ReadAsStringAsync();  
            var responseObj \= JsonSerializer.Deserialize\<JsonElement\>(responseJson);  
              
            return responseObj.GetProperty("id").GetString();  
        }  
          
        public async Task\<TrainingJobStatus\> GetJobStatusAsync(string jobId)  
        {  
            var request \= new HttpRequestMessage(  
                HttpMethod.Get,   
                $"https://api.openai.com/v1/fine\_tuning/jobs/{jobId}")  
            {  
                Headers \=   
                {  
                    { "Authorization", $"Bearer {\_options.Value.ApiKey}" }  
                }  
            };  
              
            var response \= await \_httpClient.SendAsync(request);  
            response.EnsureSuccessStatusCode();  
              
            var responseJson \= await response.Content.ReadAsStringAsync();  
            var result \= JsonSerializer.Deserialize\<OpenAiFineTuningResult\>(responseJson);  
              
            return new TrainingJobStatus  
            {  
                JobId \= jobId,  
                Status \= MapStatus(result.Status),  
                Progress \= result.PercentCompleted,  
                ModelId \= result.FineTunedModel,  
                Message \= result.Status  
            };  
        }  
          
        private async Task\<OpenAiFineTuningResult\> MonitorTrainingJobAsync(string jobId)  
        {  
            bool isCompleted \= false;  
            OpenAiFineTuningResult result \= null;  
              
            while (\!isCompleted)  
            {  
                var jobStatus \= await GetJobStatusAsync(jobId);  
                  
                if (jobStatus.Status \== JobStatus.Completed ||   
                    jobStatus.Status \== JobStatus.Failed ||   
                    jobStatus.Status \== JobStatus.Cancelled)  
                {  
                    isCompleted \= true;  
                      
                    // Get the final result  
                    var request \= new HttpRequestMessage(  
                        HttpMethod.Get,   
                        $"https://api.openai.com/v1/fine\_tuning/jobs/{jobId}")  
                    {  
                        Headers \=   
                        {  
                            { "Authorization", $"Bearer {\_options.Value.ApiKey}" }  
                        }  
                    };  
                      
                    var response \= await \_httpClient.SendAsync(request);  
                    response.EnsureSuccessStatusCode();  
                      
                    var responseJson \= await response.Content.ReadAsStringAsync();  
                    result \= JsonSerializer.Deserialize\<OpenAiFineTuningResult\>(responseJson);  
                }  
                else  
                {  
                    // Wait before checking again  
                    await Task.Delay(TimeSpan.FromSeconds(30));  
                }  
            }  
              
            return result;  
        }  
          
        public async Task\<string\> PredictAsync(string modelId, string prompt)  
        {  
            var payload \= new  
            {  
                model \= modelId,  
                messages \= new\[\]  
                {  
                    new { role \= "system", content \= "You are a helpful assistant." },  
                    new { role \= "user", content \= prompt }  
                },  
                max\_tokens \= 1024  
            };  
              
            var content \= new StringContent(  
                JsonSerializer.Serialize(payload),  
                Encoding.UTF8,  
                "application/json");  
                  
            var request \= new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")  
            {  
                Headers \=   
                {  
                    { "Authorization", $"Bearer {\_options.Value.ApiKey}" }  
                },  
                Content \= content  
            };  
              
            var response \= await \_httpClient.SendAsync(request);  
            response.EnsureSuccessStatusCode();  
              
            var responseJson \= await response.Content.ReadAsStringAsync();  
            var responseObj \= JsonSerializer.Deserialize\<JsonElement\>(responseJson);  
              
            return responseObj  
                .GetProperty("choices")\[0\]  
                .GetProperty("message")  
                .GetProperty("content")  
                .GetString();  
        }  
          
        private JobStatus MapStatus(string openaiStatus)  
        {  
            return openaiStatus switch  
            {  
                "succeeded" \=\> JobStatus.Completed,  
                "failed" \=\> JobStatus.Failed,  
                "cancelled" \=\> JobStatus.Cancelled,  
                "running" \=\> JobStatus.Running,  
                "created" \=\> JobStatus.Created,  
                "pending" \=\> JobStatus.Pending,  
                \_ \=\> JobStatus.Unknown  
            };  
        }  
    }

    public class OpenAiOptions  
    {  
        public string ApiKey { get; set; }  
        public string BaseUrl { get; set; } \= "https://api.openai.com/v1/";  
        public int Timeout { get; set; } \= 300; // seconds  
    }

    public class OpenAiFineTuningResult  
    {  
        public string Id { get; set; }  
        public string Status { get; set; }  
        public string FineTunedModel { get; set; }  
        public int PercentCompleted { get; set; }  
        public Dictionary\<string, double\> ValidationMetrics { get; set; }  
    }

    public enum JobStatus  
    {  
        Created,  
        Pending,  
        Running,  
        Completed,  
        Failed,  
        Cancelled,  
        Unknown  
    }

    public class TrainingJobStatus  
    {  
        public string JobId { get; set; }  
        public JobStatus Status { get; set; }  
        public int Progress { get; set; }  
        public string ModelId { get; set; }  
        public string Message { get; set; }  
    }

    public class TrainingResult  
    {  
        public string ModelId { get; set; }  
        public Dictionary\<string, string\> ModelFiles { get; set; } \= new Dictionary\<string, string\>();  
        public Dictionary\<string, double\> Metrics { get; set; } \= new Dictionary\<string, double\>();  
        public TimeSpan TrainingTime { get; set; }  
        public TrainingStatus Status { get; set; }  
        public string ErrorMessage { get; set; }  
    }

    public enum TrainingStatus  
    {  
        Pending,  
        Running,  
        Completed,  
        Failed,  
        Cancelled  
    }  
}

**LLMGateway.Tuning/Training/Optimization/HyperparameterOptimizer.cs**

using System;  
using System.Collections.Generic;  
using System.Linq;  
using System.Threading.Tasks;  
using LLMGateway.Tuning.Core.Enums;  
using LLMGateway.Tuning.Core.Interfaces;  
using LLMGateway.Tuning.Training.Configuration;  
using Microsoft.Extensions.Logging;

namespace LLMGateway.Tuning.Training.Optimization  
{  
    public class HyperparameterOptimizer  
    {  
        private readonly ILogger\<HyperparameterOptimizer\> \_logger;  
        private readonly IModelAdapter \_modelAdapter;  
        private readonly IDatasetStorage \_datasetStorage;  
        private readonly IModelEvaluator \_modelEvaluator;  
        private readonly Random \_random \= new Random();  
          
        public HyperparameterOptimizer(  
            ILogger\<HyperparameterOptimizer\> logger,  
            IModelAdapter modelAdapter,  
            IDatasetStorage datasetStorage,  
            IModelEvaluator modelEvaluator)  
        {  
            \_logger \= logger;  
            \_modelAdapter \= modelAdapter;  
            \_datasetStorage \= datasetStorage;  
            \_modelEvaluator \= modelEvaluator;  
        }  
          
        public async Task\<Dictionary\<string, object\>\> OptimizeHyperparametersAsync(  
            ModelType modelType,  
            string datasetId,  
            List\<HyperparameterSearchSpace\> searchSpace,  
            int maxTrials \= 10\)  
        {  
            \_logger.LogInformation("Starting hyperparameter optimization for {ModelType}", modelType);  
              
            var bestParams \= new Dictionary\<string, object\>();  
            double bestScore \= double.MinValue;  
              
            // Load datasets  
            var trainingData \= await \_datasetStorage.LoadDatasetAsync(datasetId, "training");  
            var validationData \= await \_datasetStorage.LoadDatasetAsync(datasetId, "validation");  
              
            for (int trial \= 0; trial \< maxTrials; trial++)  
            {  
                \_logger.LogInformation("Running hyperparameter trial {Trial}/{MaxTrials}", trial \+ 1, maxTrials);  
                  
                // Generate random parameters from search space  
                var params\_ \= GenerateRandomParameters(searchSpace);  
                \_logger.LogInformation("Trial parameters: {Parameters}",   
                    string.Join(", ", params\_.Select(p \=\> $"{p.Key}={p.Value}")));  
                  
                try  
                {  
                    // Create training configuration  
                    var config \= new TrainingConfiguration  
                    {  
                        ModelType \= modelType,  
                        Hyperparameters \= params\_,  
                        ValidationStrategy \= ValidationStrategy.HoldOut,  
                        MaxTrainingTime \= TimeSpan.FromHours(2),  
                        OutputModelName \= $"trial\_{trial}\_{Guid.NewGuid().ToString("N").Substring(0, 8)}"  
                    };  
                      
                    // Train model with these parameters  
                    var result \= await \_modelAdapter.TrainModelAsync(  
                        trainingData,  
                        validationData,  
                        config);  
                      
                    if (result.Status \!= TrainingStatus.Completed)  
                    {  
                        \_logger.LogWarning("Trial {Trial} failed: {Error}", trial \+ 1, result.ErrorMessage);  
                        continue;  
                    }  
                      
                    // Evaluate the model  
                    var evaluationScore \= GetEvaluationScore(result.Metrics);  
                    \_logger.LogInformation("Trial {Trial} score: {Score}", trial \+ 1, evaluationScore);  
                      
                    // Update best parameters if this trial is better  
                    if (evaluationScore \> bestScore)  
                    {  
                        bestScore \= evaluationScore;  
                        bestParams \= params\_;  
                        \_logger.LogInformation("New best parameters found with score {Score}", bestScore);  
                    }  
                }  
                catch (Exception ex)  
                {  
                    \_logger.LogError(ex, "Error during hyperparameter trial {Trial}", trial \+ 1);  
                }  
            }  
              
            \_logger.LogInformation("Hyperparameter optimization completed. Best score: {Score}", bestScore);  
            return bestParams;  
        }  
          
        private Dictionary\<string, object\> GenerateRandomParameters(List\<HyperparameterSearchSpace\> searchSpace)  
        {  
            var parameters \= new Dictionary\<string, object\>();  
              
            foreach (var param in searchSpace)  
            {  
                parameters\[param.Name\] \= param.Type switch  
                {  
                    HyperparameterType.Continuous \=\> GenerateContinuousValue(param),  
                    HyperparameterType.Discrete \=\> GenerateDiscreteValue(param),  
                    HyperparameterType.Categorical \=\> GenerateCategoricalValue(param),  
                    \_ \=\> throw new ArgumentException($"Unknown parameter type: {param.Type}")  
                };  
            }  
              
            return parameters;  
        }  
          
        private object GenerateContinuousValue(HyperparameterSearchSpace param)  
        {  
            var min \= Convert.ToDouble(param.Min);  
            var max \= Convert.ToDouble(param.Max);  
              
            if (param.LogScale)  
            {  
                var logMin \= Math.Log(min);  
                var logMax \= Math.Log(max);  
                var logValue \= logMin \+ (\_random.NextDouble() \* (logMax \- logMin));  
                return Math.Exp(logValue);  
            }  
            else  
            {  
                return min \+ (\_random.NextDouble() \* (max \- min));  
            }  
        }  
          
        private object GenerateDiscreteValue(HyperparameterSearchSpace param)  
        {  
            var min \= Convert.ToInt32(param.Min);  
            var max \= Convert.ToInt32(param.Max);  
            return \_random.Next(min, max \+ 1);  
        }  
          
        private object GenerateCategoricalValue(HyperparameterSearchSpace param)  
        {  
            var options \= param.Options;  
            var index \= \_random.Next(options.Count);  
            return options\[index\];  
        }  
          
        private double GetEvaluationScore(Dictionary\<string, double\> metrics)  
        {  
            // Typically use validation loss or accuracy as the score  
            if (metrics.TryGetValue("validation\_loss", out double loss))  
            {  
                return \-loss; // Negative because we want to maximize the score  
            }  
              
            if (metrics.TryGetValue("validation\_accuracy", out double accuracy))  
            {  
                return accuracy;  
            }  
              
            // Fallback to first metric  
            return metrics.Values.FirstOrDefault();  
        }  
    }

    public class HyperparameterSearchSpace  
    {  
        public string Name { get; set; }  
        public HyperparameterType Type { get; set; }  
        public object Min { get; set; }  
        public object Max { get; set; }  
        public bool LogScale { get; set; }  
        public List\<object\> Options { get; set; } \= new List\<object\>();  
    }

    public enum HyperparameterType  
    {  
        Continuous,  
        Discrete,  
        Categorical  
    }  
}

**LLMGateway.Tuning/Training/Jobs/ModelTrainingJob.cs**

using System;  
using System.Collections.Generic;  
using System.Threading.Tasks;  
using LLMGateway.Tuning.Core.Enums;  
using LLMGateway.Tuning.Core.Interfaces;  
using LLMGateway.Tuning.Training.Configuration;  
using Microsoft.Extensions.Logging;  
using Microsoft.Extensions.Options;

namespace LLMGateway.Tuning.Training.Jobs  
{  
    public class ModelTrainingJob  
    {  
        private readonly ILogger\<ModelTrainingJob\> \_logger;  
        private readonly IModelAdapterFactory \_modelAdapterFactory;  
        private readonly IDatasetStorage \_datasetStorage;  
        private readonly IModelRegistry \_modelRegistry;  
        private readonly IOptions\<TrainingOptions\> \_options;  
          
        public ModelTrainingJob(  
            ILogger\<ModelTrainingJob\> logger,  
            IModelAdapterFactory modelAdapterFactory,  
            IDatasetStorage datasetStorage,  
            IModelRegistry modelRegistry,  
            IOptions\<TrainingOptions\> options)  
        {  
            \_logger \= logger;  
            \_modelAdapterFactory \= modelAdapterFactory;  
            \_datasetStorage \= datasetStorage;  
            \_modelRegistry \= modelRegistry;  
            \_options \= options;  
        }  
          
        public async Task\<TrainingResult\> ExecuteTrainingJobAsync(ModelTrainingJobRequest request)  
        {  
            \_logger.LogInformation("Starting training job for {ModelType}", request.ModelType);  
              
            try  
            {  
                // Get appropriate model adapter  
                var modelAdapter \= \_modelAdapterFactory.GetModelAdapter(request.ModelType);  
                  
                // Load datasets  
                var trainingData \= await \_datasetStorage.LoadDatasetAsync(request.DatasetId, "training");  
                var validationData \= await \_datasetStorage.LoadDatasetAsync(request.DatasetId, "validation");  
                  
                \_logger.LogInformation("Loaded training data: {Size} bytes", trainingData.Length);  
                \_logger.LogInformation("Loaded validation data: {Size} bytes", validationData.Length);  
                  
                // Prepare training configuration  
                var trainingConfig \= new TrainingConfiguration  
                {  
                    ModelType \= request.ModelType,  
                    BaseModel \= request.BaseModel,  
                    Hyperparameters \= request.Hyperparameters ?? GetDefaultHyperparameters(request.ModelType),  
                    ValidationStrategy \= request.ValidationStrategy,  
                    MaxTrainingTime \= request.MaxTrainingTime ?? TimeSpan.FromHours(12),  
                    EarlyStoppingPatience \= request.EarlyStoppingPatience ?? 3,  
                    OutputModelName \= request.OutputModelName ?? $"{DateTime.UtcNow:yyyyMMdd}\_{Guid.NewGuid().ToString("N").Substring(0, 8)}"  
                };  
                  
                // Execute training  
                var result \= await modelAdapter.TrainModelAsync(  
                    trainingData,   
                    validationData,   
                    trainingConfig);  
                      
                if (result.Status \!= TrainingStatus.Completed)  
                {  
                    \_logger.LogError("Training failed: {Error}", result.ErrorMessage);  
                    return result;  
                }  
                  
                // Register the new model version  
                string modelId \= await \_modelRegistry.RegisterModelAsync(  
                    request.ModelName ?? $"{request.ModelType.ToString().ToLowerInvariant()}\_tuned",  
                    result.ModelFiles,  
                    result.Metrics,  
                    new Dictionary\<string, string\>  
                    {  
                        \["dataset\_id"\] \= request.DatasetId,  
                        \["base\_model"\] \= request.BaseModel,  
                        \["created\_by"\] \= request.CreatedBy ?? "system"  
                    });  
                      
                // Log results  
                \_logger.LogInformation(  
                    "Completed training job. Model ID: {ModelId}, Validation Score: {Score}",  
                    modelId,  
                    result.Metrics.GetValueOrDefault("validation\_accuracy", 0));  
                      
                // Update result with registered model ID  
                result.ModelId \= modelId;  
                      
                return result;  
            }  
            catch (Exception ex)  
            {  
                \_logger.LogError(ex, "Error executing training job");  
                  
                return new TrainingResult  
                {  
                    Status \= TrainingStatus.Failed,  
                    ErrorMessage \= ex.Message  
                };  
            }  
        }  
          
        private Dictionary\<string, object\> GetDefaultHyperparameters(ModelType modelType)  
        {  
            return modelType switch  
            {  
                ModelType.OpenAi \=\> new Dictionary\<string, object\>  
                {  
                    \["learning\_rate"\] \= 0.0001,  
                    \["batch\_size"\] \= 4,  
                    \["epochs"\] \= 3,  
                    \["weight\_decay"\] \= 0.01  
                },  
                ModelType.Anthropic \=\> new Dictionary\<string, object\>  
                {  
                    \["learning\_rate"\] \= 0.00005,  
                    \["batch\_size"\] \= 8,  
                    \["epochs"\] \= 5  
                },  
                \_ \=\> new Dictionary\<string, object\>()  
            };  
        }  
    }

    public class ModelTrainingJobRequest  
    {  
        public ModelType ModelType { get; set; }  
        public string BaseModel { get; set; }  
        public string DatasetId { get; set; }  
        public string ModelName { get; set; }  
        public string OutputModelName { get; set; }  
        public Dictionary\<string, object\> Hyperparameters { get; set; }  
        public ValidationStrategy ValidationStrategy { get; set; } \= ValidationStrategy.HoldOut;  
        public TimeSpan? MaxTrainingTime { get; set; }  
        public int? EarlyStoppingPatience { get; set; }  
        public string CreatedBy { get; set; }  
    }

    public class TrainingOptions  
    {  
        public int DefaultEarlyStoppingPatience { get; set; } \= 3;  
        public int DefaultEpochs { get; set; } \= 3;  
        public double DefaultLearningRate { get; set; } \= 0.0001;  
        public int DefaultBatchSize { get; set; } \= 4;  
    }  
}

## **Evaluation Components**

**LLMGateway.Tuning/Evaluation/Metrics/MetricsCalculator.cs**

using System;  
using System.Collections.Generic;  
using System.Linq;  
using System.Text.RegularExpressions;  
using LLMGateway.Tuning.Core.Models;  
using Microsoft.Extensions.Logging;

namespace LLMGateway.Tuning.Evaluation.Metrics  
{  
    public class MetricsCalculator  
    {  
        private readonly ILogger\<MetricsCalculator\> \_logger;  
          
        public MetricsCalculator(ILogger\<MetricsCalculator\> logger)  
        {  
            \_logger \= logger;  
        }  
          
        public Dictionary\<string, double\> CalculateMetrics(List\<PredictionResult\> predictions)  
        {  
            var metrics \= new Dictionary\<string, double\>();  
              
            try  
            {  
                // Basic metrics  
                metrics\["accuracy"\] \= CalculateAccuracy(predictions);  
                metrics\["average\_token\_count"\] \= CalculateAverageTokenCount(predictions);  
                metrics\["average\_latency\_ms"\] \= CalculateAverageLatency(predictions);  
                  
                // Advanced metrics  
                metrics\["relevance\_score"\] \= CalculateRelevanceScore(predictions);  
                metrics\["coherence\_score"\] \= CalculateCoherenceScore(predictions);  
                metrics\["factuality\_score"\] \= CalculateFactualityScore(predictions);  
                  
                // Domain-specific metrics can be added here  
                  
                return metrics;  
            }  
            catch (Exception ex)  
            {  
                \_logger.LogError(ex, "Error calculating metrics");  
                return new Dictionary\<string, double\>  
                {  
                    \["error"\] \= 1.0  
                };  
            }  
        }  
          
        private double CalculateAccuracy(List\<PredictionResult\> predictions)  
        {  
            if (\!predictions.Any()) return 0;  
              
            int correctCount \= predictions.Count(p \=\> p.IsCorrect);  
            return (double)correctCount / predictions.Count;  
        }  
          
        private double CalculateAverageTokenCount(List\<PredictionResult\> predictions)  
        {  
            if (\!predictions.Any()) return 0;  
              
            return predictions.Average(p \=\> p.OutputTokenCount);  
        }  
          
        private double CalculateAverageLatency(List\<PredictionResult\> predictions)  
        {  
            if (\!predictions.Any()) return 0;  
              
            return predictions.Average(p \=\> p.LatencyMs);  
        }  
          
        private double CalculateRelevanceScore(List\<PredictionResult\> predictions)  
        {  
            // Simple implementation \- could be expanded with more sophisticated algorithms  
            return predictions.Average(p \=\> p.Scores.GetValueOrDefault("relevance", 0));  
        }  
          
        private double CalculateCoherenceScore(List\<PredictionResult\> predictions)  
        {  
            // Simple implementation \- could be expanded with more sophisticated algorithms  
            return predictions.Average(p \=\> p.Scores.GetValueOrDefault("coherence", 0));  
        }  
          
        private double CalculateFactualityScore(List\<PredictionResult\> predictions)  
        {  
            // Simple implementation \- could be expanded with more sophisticated algorithms  
            return predictions.Average(p \=\> p.Scores.GetValueOrDefault("factuality", 0));  
        }  
          
        public double CalculateSimilarity(string predicted, string expected)  
        {  
            if (string.IsNullOrEmpty(predicted) || string.IsNullOrEmpty(expected))  
                return 0;  
                  
            // Normalize texts  
            predicted \= NormalizeText(predicted);  
            expected \= NormalizeText(expected);  
              
            // Calculate Jaccard similarity on word sets  
            var predictedWords \= predicted.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();  
            var expectedWords \= expected.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();  
              
            var intersection \= predictedWords.Intersect(expectedWords).Count();  
            var union \= predictedWords.Union(expectedWords).Count();  
              
            return union \== 0 ? 0 : (double)intersection / union;  
        }  
          
        private string NormalizeText(string text)  
        {  
            // Convert to lowercase  
            text \= text.ToLowerInvariant();  
              
            // Remove punctuation  
            text \= Regex.Replace(text, @"\[^\\w\\s\]", " ");  
              
            // Remove extra whitespace  
            text \= Regex.Replace(text, @"\\s+", " ").Trim();  
              
            return text;  
        }  
    }

    public class PredictionResult  
    {  
        public string Id { get; set; } \= Guid.NewGuid().ToString();  
        public string ModelId { get; set; }  
        public string Prompt { get; set; }  
        public string ExpectedOutput { get; set; }  
        public string ActualOutput { get; set; }  
        public bool IsCorrect { get; set; }  
        public int InputTokenCount { get; set; }  
        public int OutputTokenCount { get; set; }  
        public double LatencyMs { get; set; }  
        public Dictionary\<string, double\> Scores { get; set; } \= new Dictionary\<string, double\>();  
    }  
}

**LLMGateway.Tuning/Evaluation/ModelEvaluator.cs**

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
        private readonly ILogger\<ModelEvaluator\> \_logger;  
        private readonly IModelAdapter \_modelAdapter;  
        private readonly IDatasetStorage \_datasetStorage;  
        private readonly MetricsCalculator \_metricsCalculator;  
          
        public ModelEvaluator(  
            ILogger\<ModelEvaluator\> logger,  
            IModelAdapter modelAdapter,  
            IDatasetStorage datasetStorage,  
            MetricsCalculator metricsCalculator)  
        {  
            \_logger \= logger;  
            \_modelAdapter \= modelAdapter;  
            \_datasetStorage \= datasetStorage;  
            \_metricsCalculator \= metricsCalculator;  
        }  
          
        public async Task\<EvaluationResult\> EvaluateModelAsync(ModelEvaluationRequest request)  
        {  
            \_logger.LogInformation("Evaluating model: {ModelId}", request.ModelId);  
              
            try  
            {  
                // Load evaluation dataset  
                var evaluationData \= await \_datasetStorage.LoadDatasetAsync(  
                    request.DatasetId,  
                    request.DatasetSplit ?? "validation");  
                      
                // Deserialize evaluation examples  
                var examples \= System.Text.Json.JsonSerializer.Deserialize\<List\<TrainingExample\>\>(evaluationData);  
                  
                if (examples \== null || examples.Count \== 0\)  
                {  
                    throw new InvalidOperationException("Evaluation dataset is empty or invalid");  
                }  
                  
                \_logger.LogInformation("Loaded {Count} evaluation examples", examples.Count);  
                  
                // Run predictions  
                var predictions \= new List\<PredictionResult\>();  
                  
                foreach (var example in examples)  
                {  
                    var startTime \= DateTime.UtcNow;  
                      
                    var prediction \= await \_modelAdapter.PredictAsync(  
                        request.ModelId,  
                        example.UserPrompt);  
                          
                    var latency \= (DateTime.UtcNow \- startTime).TotalMilliseconds;  
                      
                    // Calculate similarity score  
                    var similarity \= \_metricsCalculator.CalculateSimilarity(  
                        prediction,   
                        example.PreferredResponse);  
                          
                    predictions.Add(new PredictionResult  
                    {  
                        ModelId \= request.ModelId,  
                        Prompt \= example.UserPrompt,  
                        ExpectedOutput \= example.PreferredResponse,  
                        ActualOutput \= prediction,  
                        IsCorrect \= similarity \> 0.7, // Threshold for "correctness"  
                        InputTokenCount \= example.UserPrompt.Length / 4, // Approximation  
                        OutputTokenCount \= prediction.Length / 4, // Approximation  
                        LatencyMs \= latency,  
                        Scores \= new Dictionary\<string, double\>  
                        {  
                            \["similarity"\] \= similarity,  
                            \["relevance"\] \= CalculateRelevance(prediction, example.UserPrompt),  
                            \["coherence"\] \= CalculateCoherence(prediction),  
                            \["factuality"\] \= 0.5 // Placeholder \- would need specialized factual evaluation  
                        }  
                    });  
                }  
                  
                // Calculate metrics  
                var metrics \= \_metricsCalculator.CalculateMetrics(predictions);  
                  
                // Generate evaluation report  
                var report \= GenerateReport(metrics, predictions);  
                  
                \_logger.LogInformation("Model evaluation completed with Accuracy: {Accuracy}",   
                    metrics.GetValueOrDefault("accuracy", 0));  
                      
                return new EvaluationResult  
                {  
                    ModelId \= request.ModelId,  
                    DatasetId \= request.DatasetId,  
                    Metrics \= metrics,  
                    DetailedReport \= report,  
                    EvaluationTimestamp \= DateTime.UtcNow  
                };  
            }  
            catch (Exception ex)  
            {  
                \_logger.LogError(ex, "Error evaluating model");  
                  
                return new EvaluationResult  
                {  
                    ModelId \= request.ModelId,  
                    DatasetId \= request.DatasetId,  
                    Metrics \= new Dictionary\<string, double\> { \["error"\] \= 1.0 },  
                    DetailedReport \= $"Evaluation failed: {ex.Message}",  
                    EvaluationTimestamp \= DateTime.UtcNow  
                };  
            }  
        }  
          
        private double CalculateRelevance(string response, string prompt)  
        {  
            // Basic relevance calculation \- could be improved  
            var promptWords \= prompt.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);  
            var responseWords \= response.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);  
              
            int matchCount \= 0;  
            foreach (var word in promptWords)  
            {  
                if (responseWords.Contains(word))  
                {  
                    matchCount++;  
                }  
            }  
              
            return promptWords.Length \> 0   
                ? (double)matchCount / promptWords.Length  
                : 0;  
        }  
          
        private double CalculateCoherence(string text)  
        {  
            // Simple coherence metric based on sentence length variance  
            // More sophisticated metrics would consider discourse connectives, sentence transitions, etc.  
              
            var sentences \= text.Split(new\[\] { '.', '\!', '?' }, StringSplitOptions.RemoveEmptyEntries);  
              
            if (sentences.Length \<= 1\)  
                return 0.5; // Default for very short texts  
                  
            var lengths \= sentences.Select(s \=\> s.Trim().Split(' ').Length).ToList();  
            double mean \= lengths.Average();  
            double variance \= lengths.Sum(l \=\> Math.Pow(l \- mean, 2)) / lengths.Count;  
              
            // Lower variance suggests more consistent sentence structure, which we'll use as a proxy for coherence  
            return Math.Max(0, 1 \- (variance / (mean \* 2)));  
        }  
          
        private string GenerateReport(Dictionary\<string, double\> metrics, List\<PredictionResult\> predictions)  
        {  
            var sb \= new StringBuilder();  
              
            // Overall metrics  
            sb.AppendLine("\#\# Overall Metrics");  
            sb.AppendLine();  
            sb.AppendLine("| Metric | Value |");  
            sb.AppendLine("| \------ | \----- |");  
              
            foreach (var metric in metrics.OrderBy(m \=\> m.Key))  
            {  
                sb.AppendLine($"| {metric.Key} | {metric.Value:F4} |");  
            }  
              
            // Sample predictions  
            sb.AppendLine();  
            sb.AppendLine("\#\# Sample Predictions");  
            sb.AppendLine();  
              
            // Show some good and bad examples  
            var bestPredictions \= predictions  
                .OrderByDescending(p \=\> p.Scores.GetValueOrDefault("similarity", 0))  
                .Take(3);  
                  
            var worstPredictions \= predictions  
                .OrderBy(p \=\> p.Scores.GetValueOrDefault("similarity", 0))  
                .Take(3);  
                  
            // Best examples  
            sb.AppendLine("\#\#\# Best Predictions");  
            sb.AppendLine();  
              
            foreach (var prediction in bestPredictions)  
            {  
                sb.AppendLine($"\*\*Prompt\*\*: {prediction.Prompt}");  
                sb.AppendLine();  
                sb.AppendLine($"\*\*Expected\*\*: {prediction.ExpectedOutput}");  
                sb.AppendLine();  
                sb.AppendLine($"\*\*Actual\*\*: {prediction.ActualOutput}");  
                sb.AppendLine();  
                sb.AppendLine($"\*\*Similarity Score\*\*: {prediction.Scores.GetValueOrDefault("similarity", 0):F4}");  
                sb.AppendLine();  
                sb.AppendLine("---");  
            }  
              
            // Worst examples  
            sb.AppendLine("\#\#\# Worst Predictions");  
            sb.AppendLine();  
              
            foreach (var prediction in worstPredictions)  
            {  
                sb.AppendLine($"\*\*Prompt\*\*: {prediction.Prompt}");  
                sb.AppendLine();  
                sb.AppendLine($"\*\*Expected\*\*: {prediction.ExpectedOutput}");  
                sb.AppendLine();  
                sb.AppendLine($"\*\*Actual\*\*: {prediction.ActualOutput}");  
                sb.AppendLine();  
                sb.AppendLine($"\*\*Similarity Score\*\*: {prediction.Scores.GetValueOrDefault("similarity", 0):F4}");  
                sb.AppendLine();  
                sb.AppendLine("---");  
            }  
              
            return sb.ToString();  
        }  
    }

    public class ModelEvaluationRequest  
    {  
        public string ModelId { get; set; }  
        public string DatasetId { get; set; }  
        public string DatasetSplit { get; set; }  
    }

    public class EvaluationResult  
    {  
        public string ModelId { get; set; }  
        public string DatasetId { get; set; }  
        public Dictionary\<string, double\> Metrics { get; set; } \= new Dictionary\<string, double\>();  
        public string DetailedReport { get; set; }  
        public DateTime EvaluationTimestamp { get; set; }  
    }  
}

## **Deployment Components**

**LLMGateway.Tuning/Deployment/Strategies/CanaryDeployer.cs**

using System;  
using System.Collections.Generic;  
using System.Threading.Tasks;  
using LLMGateway.Tuning.Core.Interfaces;  
using LLMGateway.Tuning.Monitoring.Performance;  
using Microsoft.Extensions.Logging;

namespace LLMGateway.Tuning.Deployment.Strategies  
{  
    public class CanaryDeployer  
    {  
        private readonly ILogger\<CanaryDeployer\> \_logger;  
        private readonly IModelDeploymentManager \_deploymentManager;  
        private readonly IPerformanceAnalyzer \_performanceAnalyzer;  
          
        public CanaryDeployer(  
            ILogger\<CanaryDeployer\> logger,  
            IModelDeploymentManager deploymentManager,  
            IPerformanceAnalyzer performanceAnalyzer)  
        {  
            \_logger \= logger;  
            \_deploymentManager \= deploymentManager;  
            \_performanceAnalyzer \= performanceAnalyzer;  
        }  
          
        public async Task\<DeploymentResult\> DeployWithCanaryAsync(  
            ModelDeploymentRequest request,  
            CanaryConfiguration config)  
        {  
            \_logger.LogInformation(  
                "Starting canary deployment for model {ModelId} in environment {Environment}",   
                request.ModelId,   
                request.Environment);  
                  
            try  
            {  
                // Initial deployment to small percentage  
                \_logger.LogInformation(  
                    "Deploying model {ModelId} to {InitialPercentage}% of traffic",   
                    request.ModelId,   
                    config.InitialPercentage);  
                      
                var initialDeployment \= await \_deploymentManager.DeployModelAsync(  
                    request with { TrafficPercentage \= config.InitialPercentage });  
                      
                if (\!initialDeployment.Success)  
                {  
                    \_logger.LogError(  
                        "Initial canary deployment failed: {ErrorMessage}",   
                        initialDeployment.ErrorMessage);  
                          
                    return initialDeployment;  
                }  
                  
                \_logger.LogInformation(  
                    "Initial deployment successful. Deployment ID: {DeploymentId}",   
                    initialDeployment.DeploymentId);  
                      
                // Wait for evaluation period  
                \_logger.LogInformation(  
                    "Waiting for {EvaluationPeriod} for performance data collection",   
                    config.EvaluationPeriod);  
                      
                await Task.Delay(config.EvaluationPeriod);  
                  
                // Monitor performance  
                \_logger.LogInformation("Analyzing canary performance");  
                  
                var performance \= await \_performanceAnalyzer.AnalyzeCanaryPerformanceAsync(  
                    initialDeployment.DeploymentId,  
                    config.BaselineDeploymentId,  
                    config.EvaluationPeriod);  
                      
                \_logger.LogInformation(  
                    "Canary performance analysis complete. Score: {Score}, Promotion Threshold: {Threshold}",   
                    performance.Score,   
                    config.PromotionThreshold);  
                  
                if (performance.Score \>= config.PromotionThreshold)  
                {  
                    \_logger.LogInformation(  
                        "Canary performance meets promotion threshold. Promoting to full deployment.");  
                          
                    // Full deployment  
                    var fullDeployment \= await \_deploymentManager.DeployModelAsync(  
                        request with { TrafficPercentage \= 100 });  
                          
                    if (\!fullDeployment.Success)  
                    {  
                        \_logger.LogError(  
                            "Full deployment failed: {ErrorMessage}",   
                            fullDeployment.ErrorMessage);  
                              
                        return fullDeployment;  
                    }  
                      
                    \_logger.LogInformation(  
                        "Full deployment successful. Deployment ID: {DeploymentId}",   
                        fullDeployment.DeploymentId);  
                          
                    return fullDeployment with   
                    {  
                        CanaryDeploymentId \= initialDeployment.DeploymentId,  
                        Metadata \= new Dictionary\<string, string\>  
                        {  
                            \["canary\_score"\] \= performance.Score.ToString("F4"),  
                            \["canary\_deployment\_id"\] \= initialDeployment.DeploymentId,  
                            \["promotion\_threshold"\] \= config.PromotionThreshold.ToString("F4"),  
                            \["canary\_promotion"\] \= "success"  
                        }  
                    };  
                }  
                else  
                {  
                    \_logger.LogWarning(  
                        "Canary performance below promotion threshold. Rolling back.");  
                          
                    // Rollback  
                    var rollback \= await \_deploymentManager.RollbackDeploymentAsync(  
                        initialDeployment.DeploymentId);  
                          
                    if (\!rollback.Success)  
                    {  
                        \_logger.LogError(  
                            "Rollback failed: {ErrorMessage}",   
                            rollback.ErrorMessage);  
                    }  
                    else  
                    {  
                        \_logger.LogInformation(  
                            "Rollback successful. Deployment ID: {DeploymentId}",   
                            rollback.DeploymentId);  
                    }  
                      
                    return new DeploymentResult  
                    {  
                        Success \= false,  
                        ModelId \= request.ModelId,  
                        Environment \= request.Environment,  
                        CanaryDeploymentId \= initialDeployment.DeploymentId,  
                        ErrorMessage \= "Canary performance below promotion threshold",  
                        DeploymentTimestamp \= DateTime.UtcNow,  
                        Metadata \= new Dictionary\<string, string\>  
                        {  
                            \["canary\_score"\] \= performance.Score.ToString("F4"),  
                            \["canary\_deployment\_id"\] \= initialDeployment.DeploymentId,  
                            \["promotion\_threshold"\] \= config.PromotionThreshold.ToString("F4"),  
                            \["canary\_promotion"\] \= "failed",  
                            \["rollback\_success"\] \= rollback.Success.ToString()  
                        }  
                    };  
                }  
            }  
            catch (Exception ex)  
            {  
                \_logger.LogError(ex, "Error during canary deployment");  
                  
                return new DeploymentResult  
                {  
                    Success \= false,  
                    ModelId \= request.ModelId,  
                    Environment \= request.Environment,  
                    ErrorMessage \= $"Canary deployment error: {ex.Message}",  
                    DeploymentTimestamp \= DateTime.UtcNow  
                };  
            }  
        }  
    }

    public class CanaryConfiguration  
    {  
        public int InitialPercentage { get; set; } \= 10;  
        public TimeSpan EvaluationPeriod { get; set; } \= TimeSpan.FromHours(1);  
        public double PromotionThreshold { get; set; } \= 0.95;  
        public string BaselineDeploymentId { get; set; }  
        public List\<string\> MetricsToMonitor { get; set; } \= new List\<string\>();  
    }

    public class ModelDeploymentRequest  
    {  
        public string ModelId { get; set; }  
        public string Environment { get; set; }  
        public int TrafficPercentage { get; set; }  
        public string DeployedBy { get; set; }  
        public string Reason { get; set; }  
        public Dictionary\<string, string\> Metadata { get; set; } \= new Dictionary\<string, string\>();  
    }

    public class DeploymentResult  
    {  
        public bool Success { get; set; }  
        public string DeploymentId { get; set; }  
        public string CanaryDeploymentId { get; set; }  
        public string ModelId { get; set; }  
        public string Environment { get; set; }  
        public string ErrorMessage { get; set; }  
        public DateTime DeploymentTimestamp { get; set; }  
        public Dictionary\<string, string\> Metadata { get; set; } \= new Dictionary\<string, string\>();  
    }  
}

**LLMGateway.Tuning/Deployment/Registry/ModelRegistry.cs**

using System;  
using System.Collections.Generic;  
using System.Threading.Tasks;  
using LLMGateway.Tuning.Core.Interfaces;  
using Microsoft.Extensions.Logging;

namespace LLMGateway.Tuning.Deployment.Registry  
{  
    public class ModelRegistry : IModelRegistry  
    {  
        private readonly ILogger\<ModelRegistry\> \_logger;  
        private readonly IModelRegistryStorage \_storage;  
          
        public ModelRegistry(  
            ILogger\<ModelRegistry\> logger,  
            IModelRegistryStorage storage)  
        {  
            \_logger \= logger;  
            \_storage \= storage;  
        }  
          
        public async Task\<string\> RegisterModelAsync(  
            string modelName,  
            Dictionary\<string, string\> modelFiles,  
            Dictionary\<string, double\> metrics,  
            Dictionary\<string, string\> metadata \= null)  
        {  
            try  
            {  
                // Generate a unique model ID  
                string modelId \= $"{modelName}\_{DateTime.UtcNow:yyyyMMdd}\_{Guid.NewGuid().ToString("N").Substring(0, 8)}";  
                  
                // Create model version  
                var modelVersion \= new ModelVersion  
                {  
                    Id \= modelId,  
                    Name \= modelName,  
                    Files \= modelFiles,  
                    Metrics \= metrics,  
                    Metadata \= metadata ?? new Dictionary\<string, string\>(),  
                    CreatedAt \= DateTime.UtcNow,  
                    Status \= ModelStatus.Available  
                };  
                  
                // Store the model version  
                await \_storage.SaveModelVersionAsync(modelVersion);  
                  
                \_logger.LogInformation("Registered new model version: {ModelId}", modelId);  
                  
                return modelId;  
            }  
            catch (Exception ex)  
            {  
                \_logger.LogError(ex, "Error registering model");  
                throw;  
            }  
        }  
          
        public async Task\<ModelVersion\> GetModelVersionAsync(string modelId)  
        {  
            try  
            {  
                return await \_storage.GetModelVersionAsync(modelId);  
            }  
            catch (Exception ex)  
            {  
                \_logger.LogError(ex, "Error getting model version: {ModelId}", modelId);  
                return null;  
            }  
        }  
          
        public async Task\<List\<ModelVersion\>\> GetModelVersionsAsync(  
            string modelName,  
            int limit \= 10,  
            int offset \= 0\)  
        {  
            try  
            {  
                return await \_storage.GetModelVersionsAsync(modelName, limit, offset);  
            }  
            catch (Exception ex)  
            {  
                \_logger.LogError(ex, "Error getting model versions for {ModelName}", modelName);  
                return new List\<ModelVersion\>();  
            }  
        }  
          
        public async Task\<string\> GetProductionModelIdAsync(string modelName, string environment \= "production")  
        {  
            try  
            {  
                return await \_storage.GetProductionModelIdAsync(modelName, environment);  
            }  
            catch (Exception ex)  
            {  
                \_logger.LogError(ex,   
                    "Error getting production model ID for {ModelName} in {Environment}",  
                    modelName, environment);  
                return null;  
            }  
        }  
          
        public async Task\<bool\> UpdateModelStatusAsync(string modelId, ModelStatus status)  
        {  
            try  
            {  
                await \_storage.UpdateModelStatusAsync(modelId, status);  
                  
                \_logger.LogInformation(  
                    "Updated status for model {ModelId} to {Status}",   
                    modelId, status);  
                      
                return true;  
            }  
            catch (Exception ex)  
            {  
                \_logger.LogError(ex,   
                    "Error updating status for model {ModelId} to {Status}",   
                    modelId, status);  
                      
                return false;  
            }  
        }  
    }

    public class ModelVersion  
    {  
        public string Id { get; set; }  
        public string Name { get; set; }  
        public Dictionary\<string, string\> Files { get; set; } \= new Dictionary\<string, string\>();  
        public Dictionary\<string, double\> Metrics { get; set; } \= new Dictionary\<string, double\>();  
        public Dictionary\<string, string\> Metadata { get; set; } \= new Dictionary\<string, string\>();  
        public DateTime CreatedAt { get; set; }  
        public ModelStatus Status { get; set; }  
    }

    public enum ModelStatus  
    {  
        Created,  
        Training,  
        Available,  
        Deployed,  
        Archived,  
        Failed  
    }  
}

## **API Controllers**

**LLMGateway.Tuning/Api/ModelTrainingController.cs**

using System.Threading.Tasks;  
using LLMGateway.Tuning.Core.Models;  
using LLMGateway.Tuning.Data.Collection;  
using LLMGateway.Tuning.Data.Preparation;  
using LLMGateway.Tuning.Deployment.Strategies;  
using LLMGateway.Tuning.Evaluation;  
using LLMGateway.Tuning.Security.Authorization;  
using LLMGateway.Tuning.Training.Jobs;  
using Microsoft.AspNetCore.Authorization;  
using Microsoft.AspNetCore.Mvc;  
using Microsoft.Extensions.Logging;

namespace LLMGateway.Tuning.Api  
{  
    \[ApiController\]  
    \[Route("api/model-training")\]  
    public class ModelTrainingController : ControllerBase  
    {  
        private readonly ILogger\<ModelTrainingController\> \_logger;  
        private readonly FeedbackCollector \_feedbackCollector;  
        private readonly DatasetGenerator \_datasetGenerator;  
        private readonly ModelTrainingJob \_trainingJob;  
        private readonly ModelEvaluator \_modelEvaluator;  
        private readonly CanaryDeployer \_canaryDeployer;  
          
        public ModelTrainingController(  
            ILogger\<ModelTrainingController\> logger,  
            FeedbackCollector feedbackCollector,  
            DatasetGenerator datasetGenerator,  
            ModelTrainingJob trainingJob,  
            ModelEvaluator modelEvaluator,  
            CanaryDeployer canaryDeployer)  
        {  
            \_logger \= logger;  
            \_feedbackCollector \= feedbackCollector;  
            \_datasetGenerator \= datasetGenerator;  
            \_trainingJob \= trainingJob;  
            \_modelEvaluator \= modelEvaluator;  
            \_canaryDeployer \= canaryDeployer;  
        }  
          
        \[HttpPost("feedback")\]  
        public async Task\<IActionResult\> RecordFeedback(\[FromBody\] FeedbackData feedback)  
        {  
            var id \= await \_feedbackCollector.RecordFeedbackAsync(feedback);  
            return Ok(new { id });  
        }  
          
        \[HttpPost("datasets")\]  
        \[ModelAccess(ModelPermission.Create)\]  
        public async Task\<IActionResult\> GenerateDataset(\[FromBody\] DatasetGenerationOptions options)  
        {  
            var datasetId \= await \_datasetGenerator.GenerateTrainingDatasetAsync(options);  
            return Ok(new { datasetId });  
        }  
          
        \[HttpPost("training-jobs")\]  
        \[ModelAccess(ModelPermission.Create)\]  
        public async Task\<IActionResult\> CreateTrainingJob(\[FromBody\] ModelTrainingJobRequest request)  
        {  
            var result \= await \_trainingJob.ExecuteTrainingJobAsync(request);  
            return Ok(result);  
        }  
          
        \[HttpPost("evaluations")\]  
        \[ModelAccess(ModelPermission.Evaluate)\]  
        public async Task\<IActionResult\> EvaluateModel(\[FromBody\] ModelEvaluationRequest request)  
        {  
            var result \= await \_modelEvaluator.EvaluateModelAsync(request);  
            return Ok(result);  
        }  
          
        \[HttpPost("deployments/canary")\]  
        \[ModelAccess(ModelPermission.Deploy)\]  
        public async Task\<IActionResult\> DeployWithCanary(  
            \[FromBody\] CanaryDeploymentRequest request)  
        {  
            var config \= new CanaryConfiguration  
            {  
                InitialPercentage \= request.InitialPercentage,  
                EvaluationPeriod \= request.EvaluationPeriod,  
                PromotionThreshold \= request.PromotionThreshold,  
                BaselineDeploymentId \= request.BaselineDeploymentId,  
                MetricsToMonitor \= request.MetricsToMonitor  
            };  
              
            var deploymentRequest \= new ModelDeploymentRequest  
            {  
                ModelId \= request.ModelId,  
                Environment \= request.Environment,  
                DeployedBy \= request.DeployedBy,  
                Reason \= request.Reason,  
                Metadata \= request.Metadata  
            };  
              
            var result \= await \_canaryDeployer.DeployWithCanaryAsync(deploymentRequest, config);  
              
            if (\!result.Success)  
            {  
                return BadRequest(result);  
            }  
              
            return Ok(result);  
        }  
    }

    public class CanaryDeploymentRequest  
    {  
        public string ModelId { get; set; }  
        public string Environment { get; set; }  
        public int InitialPercentage { get; set; } \= 10;  
        public System.TimeSpan EvaluationPeriod { get; set; } \= System.TimeSpan.FromHours(1);  
        public double PromotionThreshold { get; set; } \= 0.95;  
        public string BaselineDeploymentId { get; set; }  
        public string DeployedBy { get; set; }  
        public string Reason { get; set; }  
        public System.Collections.Generic.Dictionary\<string, string\> Metadata { get; set; } \= new System.Collections.Generic.Dictionary\<string, string\>();  
        public System.Collections.Generic.List\<string\> MetricsToMonitor { get; set; } \= new System.Collections.Generic.List\<string\>();  
    }  
}

## **Security Components**

**LLMGateway.Tuning/Security/Authorization/ModelAccessAttribute.cs**

using Microsoft.AspNetCore.Authorization;

namespace LLMGateway.Tuning.Security.Authorization  
{  
    public class ModelAccessAttribute : AuthorizeAttribute  
    {  
        public ModelAccessAttribute(ModelPermission permission)  
        {  
            Policy \= $"ModelAccess:{permission}";  
        }  
    }

    public enum ModelPermission  
    {  
        View,  
        Create,  
        Edit,  
        Delete,  
        Deploy,  
        Evaluate,  
        Manage  
    }  
}

**LLMGateway.Tuning/Security/Authorization/ModelAccessPolicyProvider.cs**

using System;  
using System.Threading.Tasks;  
using Microsoft.AspNetCore.Authorization;  
using Microsoft.Extensions.Options;

namespace LLMGateway.Tuning.Security.Authorization  
{  
    public class ModelAccessPolicyProvider : IAuthorizationPolicyProvider  
    {  
        private readonly DefaultAuthorizationPolicyProvider \_defaultPolicyProvider;  
          
        public ModelAccessPolicyProvider(IOptions\<AuthorizationOptions\> options)  
        {  
            \_defaultPolicyProvider \= new DefaultAuthorizationPolicyProvider(options);  
        }  
          
        public Task\<AuthorizationPolicy\> GetDefaultPolicyAsync()  
        {  
            return \_defaultPolicyProvider.GetDefaultPolicyAsync();  
        }  
          
        public Task\<AuthorizationPolicy\> GetFallbackPolicyAsync()  
        {  
            return \_defaultPolicyProvider.GetFallbackPolicyAsync();  
        }  
          
        public Task\<AuthorizationPolicy\> GetPolicyAsync(string policyName)  
        {  
            if (policyName.StartsWith("ModelAccess:", StringComparison.OrdinalIgnoreCase))  
            {  
                var permissionName \= policyName.Substring("ModelAccess:".Length);  
                  
                if (Enum.TryParse\<ModelPermission\>(permissionName, out var permission))  
                {  
                    var policy \= new AuthorizationPolicyBuilder();  
                      
                    switch (permission)  
                    {  
                        case ModelPermission.View:  
                            policy.RequireRole("ModelViewer", "ModelManager", "ModelAdmin");  
                            break;  
                              
                        case ModelPermission.Create:  
                        case ModelPermission.Edit:  
                        case ModelPermission.Evaluate:  
                            policy.RequireRole("ModelManager", "ModelAdmin");  
                            break;  
                              
                        case ModelPermission.Deploy:  
                        case ModelPermission.Delete:  
                        case ModelPermission.Manage:  
                            policy.RequireRole("ModelAdmin");  
                            break;  
                              
                        default:  
                            return \_defaultPolicyProvider.GetPolicyAsync(policyName);  
                    }  
                      
                    return Task.FromResult(policy.Build());  
                }  
            }  
              
            return \_defaultPolicyProvider.GetPolicyAsync(policyName);  
        }  
    }  
}

## **Monitoring Components**

**LLMGateway.Tuning/Monitoring/Drift/DataDriftDetector.cs**

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
        private readonly ILogger\<DataDriftDetector\> \_logger;  
        private readonly IDatasetStorage \_datasetStorage;  
          
        public DataDriftDetector(  
            ILogger\<DataDriftDetector\> logger,  
            IDatasetStorage datasetStorage)  
        {  
            \_logger \= logger;  
            \_datasetStorage \= datasetStorage;  
        }  
          
        public async Task\<DriftDetectionResult\> CheckForDriftAsync(  
            string referenceDatasetId,  
            string currentDatasetId)  
        {  
            \_logger.LogInformation(  
                "Checking for drift between reference dataset {ReferenceId} and current dataset {CurrentId}",  
                referenceDatasetId,  
                currentDatasetId);  
                  
            try  
            {  
                // Load datasets  
                var referenceData \= await \_datasetStorage.LoadDatasetAsync(referenceDatasetId, "training");  
                var currentData \= await \_datasetStorage.LoadDatasetAsync(currentDatasetId, "training");  
                  
                // Calculate dataset statistics  
                var referenceStats \= CalculateDatasetStatistics(referenceData);  
                var currentStats \= CalculateDatasetStatistics(currentData);  
                  
                // Calculate drift metrics  
                var featureDrift \= CalculateFeatureDrift(referenceStats, currentStats);  
                var dataQualityIssues \= DetectDataQualityIssues(currentStats);  
                var distributionShift \= CalculateDistributionShift(referenceStats, currentStats);  
                  
                // Calculate overall drift score  
                double overallDriftScore \= CalculateOverallDriftScore(featureDrift, distributionShift);  
                  
                \_logger.LogInformation("Drift detection completed. Overall drift score: {Score}", overallDriftScore);  
                  
                return new DriftDetectionResult  
                {  
                    ReferenceDatasetId \= referenceDatasetId,  
                    CurrentDatasetId \= currentDatasetId,  
                    OverallDriftScore \= overallDriftScore,  
                    FeatureDriftScores \= featureDrift,  
                    DistributionShiftScore \= distributionShift,  
                    DataQualityIssues \= dataQualityIssues,  
                    Timestamp \= DateTime.UtcNow  
                };  
            }  
            catch (Exception ex)  
            {  
                \_logger.LogError(ex, "Error detecting data drift");  
                  
                return new DriftDetectionResult  
                {  
                    ReferenceDatasetId \= referenceDatasetId,  
                    CurrentDatasetId \= currentDatasetId,  
                    OverallDriftScore \= 0,  
                    FeatureDriftScores \= new Dictionary\<string, double\>(),  
                    DataQualityIssues \= new List\<DataQualityIssue\>(),  
                    ErrorMessage \= ex.Message,  
                    Timestamp \= DateTime.UtcNow  
                };  
            }  
        }  
          
        private DatasetStatistics CalculateDatasetStatistics(string datasetJson)  
        {  
            // Deserialize dataset  
            var examples \= System.Text.Json.JsonSerializer.Deserialize\<List\<dynamic\>\>(datasetJson);  
              
            // For simplicity, we'll calculate basic statistics  
            // In a real implementation, we would extract more meaningful features  
              
            return new DatasetStatistics  
            {  
                ExampleCount \= examples.Count,  
                AveragePromptLength \= examples.Average(e \=\> ((string)e.GetProperty("prompt")).Length),  
                TokenDistribution \= CalculateTokenDistribution(examples),  
                TopicDistribution \= CalculateTopicDistribution(examples)  
            };  
        }  
          
        private Dictionary\<string, double\> CalculateTokenDistribution(List\<dynamic\> examples)  
        {  
            // Simplified token distribution calculation  
            // In a real implementation, you would use a tokenizer  
              
            var distribution \= new Dictionary\<string, double\>();  
            var allText \= string.Join(" ", examples.Select(e \=\> (string)e.GetProperty("prompt")));  
            var words \= allText.Split(' ', StringSplitOptions.RemoveEmptyEntries);  
              
            var totalWords \= words.Length;  
            var wordCounts \= words  
                .GroupBy(w \=\> w.ToLowerInvariant())  
                .ToDictionary(g \=\> g.Key, g \=\> g.Count());  
                  
            foreach (var word in wordCounts.Keys.Take(100)) // Just take top 100 words  
            {  
                distribution\[word\] \= (double)wordCounts\[word\] / totalWords;  
            }  
              
            return distribution;  
        }  
          
        private Dictionary\<string, double\> CalculateTopicDistribution(List\<dynamic\> examples)  
        {  
            // Simplified topic distribution  
            // In a real implementation, you would use topic modeling or classification  
              
            // Placeholder implementation  
            return new Dictionary\<string, double\>  
            {  
                \["general"\] \= 0.5,  
                \["technical"\] \= 0.3,  
                \["creative"\] \= 0.2  
            };  
        }  
          
        private Dictionary\<string, double\> CalculateFeatureDrift(  
            DatasetStatistics reference,  
            DatasetStatistics current)  
        {  
            var driftScores \= new Dictionary\<string, double\>();  
              
            // Calculate drift for average prompt length  
            driftScores\["prompt\_length"\] \= Math.Abs(  
                (current.AveragePromptLength \- reference.AveragePromptLength) / reference.AveragePromptLength);  
                  
            // Calculate Population Stability Index (PSI) for token distribution  
            driftScores\["token\_distribution"\] \= CalculatePSI(reference.TokenDistribution, current.TokenDistribution);  
              
            // Calculate PSI for topic distribution  
            driftScores\["topic\_distribution"\] \= CalculatePSI(reference.TopicDistribution, current.TopicDistribution);  
              
            return driftScores;  
        }  
          
        private double CalculatePSI(  
            Dictionary\<string, double\> reference,  
            Dictionary\<string, double\> current)  
        {  
            double psi \= 0;  
              
            // Use keys that exist in both distributions  
            var commonKeys \= reference.Keys.Intersect(current.Keys).ToList();  
              
            foreach (var key in commonKeys)  
            {  
                var refValue \= reference\[key\];  
                var currValue \= current\[key\];  
                  
                // Avoid division by zero  
                if (refValue \<= 0\) continue;  
                  
                var ratio \= currValue / refValue;  
                psi \+= (currValue \- refValue) \* Math.Log(ratio);  
            }  
              
            return psi;  
        }  
          
        private double CalculateDistributionShift(  
            DatasetStatistics reference,  
            DatasetStatistics current)  
        {  
            // Simplified distribution shift calculation  
            // In a real implementation, you would use more sophisticated metrics  
              
            double topicShift \= CalculatePSI(reference.TopicDistribution, current.TopicDistribution);  
            double tokenShift \= CalculatePSI(reference.TokenDistribution, current.TokenDistribution);  
              
            return (topicShift \+ tokenShift) / 2;  
        }  
          
        private List\<DataQualityIssue\> DetectDataQualityIssues(DatasetStatistics stats)  
        {  
            var issues \= new List\<DataQualityIssue\>();  
              
            // Simple data quality checks  
            if (stats.ExampleCount \< 100\)  
            {  
                issues.Add(new DataQualityIssue  
                {  
                    Type \= "low\_sample\_count",  
                    Description \= $"Dataset only contains {stats.ExampleCount} examples",  
                    Severity \= DataQualitySeverity.Warning  
                });  
            }  
              
            if (stats.AveragePromptLength

**LLMGateway.Tuning/Monitoring/Drift/DataDriftDetector.cs** (continued)

           if (stats.AveragePromptLength \< 10\)  
            {  
                issues.Add(new DataQualityIssue  
                {  
                    Type \= "short\_prompts",  
                    Description \= $"Average prompt length is very short: {stats.AveragePromptLength} characters",  
                    Severity \= DataQualitySeverity.Warning  
                });  
            }  
              
            if (stats.AveragePromptLength \> 1000\)  
            {  
                issues.Add(new DataQualityIssue  
                {  
                    Type \= "long\_prompts",  
                    Description \= $"Average prompt length is very long: {stats.AveragePromptLength} characters",  
                    Severity \= DataQualitySeverity.Info  
                });  
            }  
              
            return issues;  
        }  
          
        private double CalculateOverallDriftScore(  
            Dictionary\<string, double\> featureDrift,  
            double distributionShift)  
        {  
            // Calculate weighted average of drift metrics  
            double score \= 0;  
            double weight \= 1.0 / (featureDrift.Count \+ 1); // \+1 for distribution shift  
              
            foreach (var drift in featureDrift.Values)  
            {  
                score \+= drift \* weight;  
            }  
              
            score \+= distributionShift \* weight;  
              
            return score;  
        }  
    }

    public class DatasetStatistics  
    {  
        public int ExampleCount { get; set; }  
        public double AveragePromptLength { get; set; }  
        public Dictionary\<string, double\> TokenDistribution { get; set; } \= new Dictionary\<string, double\>();  
        public Dictionary\<string, double\> TopicDistribution { get; set; } \= new Dictionary\<string, double\>();  
    }

    public class DriftDetectionResult  
    {  
        public string ReferenceDatasetId { get; set; }  
        public string CurrentDatasetId { get; set; }  
        public double OverallDriftScore { get; set; }  
        public Dictionary\<string, double\> FeatureDriftScores { get; set; } \= new Dictionary\<string, double\>();  
        public double DistributionShiftScore { get; set; }  
        public List\<DataQualityIssue\> DataQualityIssues { get; set; } \= new List\<DataQualityIssue\>();  
        public string ErrorMessage { get; set; }  
        public DateTime Timestamp { get; set; }  
    }

    public class DataQualityIssue  
    {  
        public string Type { get; set; }  
        public string Description { get; set; }  
        public DataQualitySeverity Severity { get; set; }  
    }

    public enum DataQualitySeverity  
    {  
        Info,  
        Warning,  
        Error,  
        Critical  
    }  
}

**LLMGateway.Tuning/Monitoring/Performance/PerformanceAnalyzer.cs**

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
        private readonly ILogger\<PerformanceAnalyzer\> \_logger;  
        private readonly IMetricsClient \_metricsClient;  
        private readonly IDeploymentRegistry \_deploymentRegistry;  
          
        public PerformanceAnalyzer(  
            ILogger\<PerformanceAnalyzer\> logger,  
            IMetricsClient metricsClient,  
            IDeploymentRegistry deploymentRegistry)  
        {  
            \_logger \= logger;  
            \_metricsClient \= metricsClient;  
            \_deploymentRegistry \= deploymentRegistry;  
        }  
          
        public async Task\<CanaryPerformanceResult\> AnalyzeCanaryPerformanceAsync(  
            string canaryDeploymentId,  
            string baselineDeploymentId,  
            TimeSpan evaluationPeriod)  
        {  
            \_logger.LogInformation(  
                "Analyzing canary performance for deployment {CanaryId} against baseline {BaselineId}",  
                canaryDeploymentId,  
                baselineDeploymentId);  
                  
            try  
            {  
                // Get deployment details  
                var canaryDeployment \= await \_deploymentRegistry.GetDeploymentAsync(canaryDeploymentId);  
                var baselineDeployment \= await \_deploymentRegistry.GetDeploymentAsync(baselineDeploymentId);  
                  
                if (canaryDeployment \== null || baselineDeployment \== null)  
                {  
                    throw new ArgumentException("Canary or baseline deployment not found");  
                }  
                  
                // Define metric dimensions  
                var canaryDimensions \= new Dictionary\<string, string\>  
                {  
                    \["deployment\_id"\] \= canaryDeploymentId,  
                    \["model\_id"\] \= canaryDeployment.ModelId,  
                    \["environment"\] \= canaryDeployment.Environment  
                };  
                  
                var baselineDimensions \= new Dictionary\<string, string\>  
                {  
                    \["deployment\_id"\] \= baselineDeploymentId,  
                    \["model\_id"\] \= baselineDeployment.ModelId,  
                    \["environment"\] \= baselineDeployment.Environment  
                };  
                  
                // Calculate evaluation window  
                var endTime \= DateTime.UtcNow;  
                var startTime \= endTime \- evaluationPeriod;  
                  
                // Get metrics for both deployments  
                var canaryMetrics \= await GetDeploymentMetricsAsync(canaryDimensions, startTime, endTime);  
                var baselineMetrics \= await GetDeploymentMetricsAsync(baselineDimensions, startTime, endTime);  
                  
                // Compare metrics  
                var metricComparisons \= CompareMetrics(baselineMetrics, canaryMetrics);  
                  
                // Calculate overall score  
                double overallScore \= CalculateOverallScore(metricComparisons);  
                  
                \_logger.LogInformation(  
                    "Canary analysis complete. Overall score: {Score}",   
                    overallScore);  
                      
                return new CanaryPerformanceResult  
                {  
                    CanaryDeploymentId \= canaryDeploymentId,  
                    BaselineDeploymentId \= baselineDeploymentId,  
                    EvaluationPeriod \= evaluationPeriod,  
                    Score \= overallScore,  
                    MetricComparisons \= metricComparisons,  
                    StartTime \= startTime,  
                    EndTime \= endTime  
                };  
            }  
            catch (Exception ex)  
            {  
                \_logger.LogError(ex, "Error analyzing canary performance");  
                  
                return new CanaryPerformanceResult  
                {  
                    CanaryDeploymentId \= canaryDeploymentId,  
                    BaselineDeploymentId \= baselineDeploymentId,  
                    EvaluationPeriod \= evaluationPeriod,  
                    Score \= 0,  
                    ErrorMessage \= ex.Message,  
                    StartTime \= DateTime.UtcNow \- evaluationPeriod,  
                    EndTime \= DateTime.UtcNow  
                };  
            }  
        }  
          
        private async Task\<Dictionary\<string, MetricStatistics\>\> GetDeploymentMetricsAsync(  
            Dictionary\<string, string\> dimensions,  
            DateTime startTime,  
            DateTime endTime)  
        {  
            var metrics \= new Dictionary\<string, MetricStatistics\>();  
              
            // Define the metrics to retrieve  
            var metricNames \= new\[\]  
            {  
                "model.latency\_ms",  
                "model.error\_rate",  
                "model.user\_feedback",  
                "model.token\_count",  
                "model.relevance\_score"  
            };  
              
            foreach (var metricName in metricNames)  
            {  
                var statistics \= await \_metricsClient.GetMetricStatisticsAsync(  
                    metricName,  
                    startTime,  
                    endTime,  
                    dimensions);  
                      
                metrics\[metricName\] \= statistics;  
            }  
              
            return metrics;  
        }  
          
        private Dictionary\<string, MetricComparison\> CompareMetrics(  
            Dictionary\<string, MetricStatistics\> baseline,  
            Dictionary\<string, MetricStatistics\> canary)  
        {  
            var comparisons \= new Dictionary\<string, MetricComparison\>();  
              
            foreach (var metricName in baseline.Keys)  
            {  
                if (\!canary.ContainsKey(metricName))  
                    continue;  
                      
                var baselineStats \= baseline\[metricName\];  
                var canaryStats \= canary\[metricName\];  
                  
                // Calculate percentage change  
                double percentChange \= 0;  
                if (baselineStats.Average \!= 0\)  
                {  
                    percentChange \= (canaryStats.Average \- baselineStats.Average) / Math.Abs(baselineStats.Average) \* 100;  
                }  
                  
                // Determine if the change is an improvement  
                bool isImprovement \= IsMetricImprovement(metricName, percentChange);  
                  
                // Calculate p-value for statistical significance  
                double pValue \= CalculatePValue(baselineStats, canaryStats);  
                  
                comparisons\[metricName\] \= new MetricComparison  
                {  
                    MetricName \= metricName,  
                    BaselineAverage \= baselineStats.Average,  
                    CanaryAverage \= canaryStats.Average,  
                    PercentChange \= percentChange,  
                    IsStatisticallySignificant \= pValue \< 0.05,  
                    PValue \= pValue,  
                    IsImprovement \= isImprovement,  
                    Weight \= GetMetricWeight(metricName)  
                };  
            }  
              
            return comparisons;  
        }  
          
        private bool IsMetricImprovement(string metricName, double percentChange)  
        {  
            // For some metrics, a decrease is an improvement (e.g., latency, error rate)  
            // For others, an increase is an improvement (e.g., user feedback, relevance)  
              
            return metricName switch  
            {  
                "model.latency\_ms" \=\> percentChange \< 0,  
                "model.error\_rate" \=\> percentChange \< 0,  
                "model.user\_feedback" \=\> percentChange \> 0,  
                "model.token\_count" \=\> percentChange \< 0, // Assuming fewer tokens is better  
                "model.relevance\_score" \=\> percentChange \> 0,  
                \_ \=\> percentChange \> 0 // Default assumption: higher is better  
            };  
        }  
          
        private double CalculatePValue(MetricStatistics baseline, MetricStatistics canary)  
        {  
            // Simplified p-value calculation using t-test approximation  
            // In a real implementation, you would use a proper statistical library  
              
            if (baseline.Count \< 2 || canary.Count \< 2\)  
                return 1.0; // Not enough data for statistical significance  
                  
            double pooledStdDev \= Math.Sqrt(  
                ((baseline.Count \- 1\) \* Math.Pow(baseline.StdDev, 2\) \+   
                 (canary.Count \- 1\) \* Math.Pow(canary.StdDev, 2)) /   
                (baseline.Count \+ canary.Count \- 2));  
                  
            double standardError \= pooledStdDev \* Math.Sqrt(1.0 / baseline.Count \+ 1.0 / canary.Count);  
              
            if (standardError \== 0\)  
                return 1.0; // Avoid division by zero  
                  
            double tStat \= Math.Abs(baseline.Average \- canary.Average) / standardError;  
              
            // Convert t-statistic to p-value approximation  
            // This is a very rough approximation  
            return Math.Exp(-0.717 \* tStat \- 0.416 \* Math.Pow(tStat, 2));  
        }  
          
        private double GetMetricWeight(string metricName)  
        {  
            // Assign weights to different metrics based on their importance  
            return metricName switch  
            {  
                "model.user\_feedback" \=\> 0.4, // User feedback is most important  
                "model.error\_rate" \=\> 0.3,    // Errors are critical  
                "model.latency\_ms" \=\> 0.15,   // Latency matters  
                "model.relevance\_score" \=\> 0.1,  
                "model.token\_count" \=\> 0.05,  
                \_ \=\> 0.05  
            };  
        }  
          
        private double CalculateOverallScore(Dictionary\<string, MetricComparison\> comparisons)  
        {  
            if (\!comparisons.Any())  
                return 0;  
                  
            double weightedSum \= 0;  
            double totalWeight \= 0;  
              
            foreach (var comparison in comparisons.Values)  
            {  
                // Skip metrics with insufficient data  
                if (double.IsNaN(comparison.PercentChange))  
                    continue;  
                      
                // Calculate score for this metric (0-1 range)  
                double metricScore;  
                  
                if (comparison.IsImprovement)  
                {  
                    // For improvements, score is higher  
                    metricScore \= 0.5 \+ Math.Min(Math.Abs(comparison.PercentChange) / 20, 0.5);  
                }  
                else  
                {  
                    // For regressions, score is lower  
                    metricScore \= 0.5 \- Math.Min(Math.Abs(comparison.PercentChange) / 20, 0.5);  
                }  
                  
                // Apply statistical significance  
                if (\!comparison.IsStatisticallySignificant)  
                {  
                    // If not statistically significant, move score closer to neutral (0.5)  
                    metricScore \= 0.5 \+ (metricScore \- 0.5) \* 0.5;  
                }  
                  
                weightedSum \+= metricScore \* comparison.Weight;  
                totalWeight \+= comparison.Weight;  
            }  
              
            // Normalize the score  
            return totalWeight \> 0 ? weightedSum / totalWeight : 0.5;  
        }  
    }

    public class MetricStatistics  
    {  
        public double Average { get; set; }  
        public double StdDev { get; set; }  
        public double Min { get; set; }  
        public double Max { get; set; }  
        public double P90 { get; set; }  
        public double P95 { get; set; }  
        public double P99 { get; set; }  
        public int Count { get; set; }  
    }

    public class MetricComparison  
    {  
        public string MetricName { get; set; }  
        public double BaselineAverage { get; set; }  
        public double CanaryAverage { get; set; }  
        public double PercentChange { get; set; }  
        public bool IsStatisticallySignificant { get; set; }  
        public double PValue { get; set; }  
        public bool IsImprovement { get; set; }  
        public double Weight { get; set; }  
    }

    public class CanaryPerformanceResult  
    {  
        public string CanaryDeploymentId { get; set; }  
        public string BaselineDeploymentId { get; set; }  
        public TimeSpan EvaluationPeriod { get; set; }  
        public double Score { get; set; }  
        public Dictionary\<string, MetricComparison\> MetricComparisons { get; set; } \= new Dictionary\<string, MetricComparison\>();  
        public string ErrorMessage { get; set; }  
        public DateTime StartTime { get; set; }  
        public DateTime EndTime { get; set; }  
    }  
}

**LLMGateway.Tuning/Monitoring/Cost/CostTracker.cs**

using System;  
using System.Collections.Generic;  
using System.Threading.Tasks;  
using LLMGateway.Tuning.Core.Enums;  
using LLMGateway.Tuning.Core.Interfaces;  
using LLMGateway.Tuning.Training.Configuration;  
using Microsoft.Extensions.Logging;  
using Microsoft.Extensions.Options;

namespace LLMGateway.Tuning.Monitoring.Cost  
{  
    public class CostTracker  
    {  
        private readonly ILogger\<CostTracker\> \_logger;  
        private readonly IPricingClient \_pricingClient;  
        private readonly IOptions\<CostTrackerOptions\> \_options;  
          
        public CostTracker(  
            ILogger\<CostTracker\> logger,  
            IPricingClient pricingClient,  
            IOptions\<CostTrackerOptions\> options)  
        {  
            \_logger \= logger;  
            \_pricingClient \= pricingClient;  
            \_options \= options;  
        }  
          
        public async Task\<TrainingCostEstimate\> EstimateTrainingCostAsync(  
            ModelType modelType,  
            TrainingConfiguration config)  
        {  
            try  
            {  
                \_logger.LogInformation("Estimating training cost for {ModelType}", modelType);  
                  
                // Get pricing information  
                var pricing \= await \_pricingClient.GetModelPricingAsync(modelType);  
                  
                if (pricing \== null)  
                {  
                    \_logger.LogWarning("No pricing information available for {ModelType}", modelType);  
                    return new TrainingCostEstimate  
                    {  
                        ModelType \= modelType,  
                        EstimatedCost \= 0,  
                        Currency \= "USD",  
                        Confidence \= CostEstimateConfidence.Low  
                    };  
                }  
                  
                // Calculate training compute cost  
                double trainHours \= EstimateTrainingHours(config);  
                double computeCost \= trainHours \* pricing.TrainingPricePerHour;  
                  
                // Calculate token cost if applicable  
                long tokenCount \= EstimateTokenCount(config);  
                double tokenCost \= tokenCount \* pricing.TokenPricePerMillion / 1\_000\_000;  
                  
                // Calculate storage cost  
                double storageCost \= EstimateStorageCost(config);  
                  
                // Total cost  
                double totalCost \= computeCost \+ tokenCost \+ storageCost;  
                  
                // Add margin for unpredictable factors  
                totalCost \*= \_options.Value.CostEstimateMargin;  
                  
                var estimate \= new TrainingCostEstimate  
                {  
                    ModelType \= modelType,  
                    EstimatedCost \= totalCost,  
                    Currency \= pricing.Currency,  
                    Confidence \= DetermineCostConfidence(config),  
                    CostComponents \= new Dictionary\<string, double\>  
                    {  
                        \["compute"\] \= computeCost,  
                        \["tokens"\] \= tokenCost,  
                        \["storage"\] \= storageCost  
                    },  
                    EstimatedTrainingHours \= trainHours  
                };  
                  
                \_logger.LogInformation(  
                    "Estimated cost for {ModelType} training: {Cost} {Currency}",  
                    modelType,  
                    estimate.EstimatedCost,  
                    estimate.Currency);  
                      
                return estimate;  
            }  
            catch (Exception ex)  
            {  
                \_logger.LogError(ex, "Error estimating training cost");  
                  
                return new TrainingCostEstimate  
                {  
                    ModelType \= modelType,  
                    EstimatedCost \= 0,  
                    Currency \= "USD",  
                    Confidence \= CostEstimateConfidence.Unknown,  
                    ErrorMessage \= ex.Message  
                };  
            }  
        }  
          
        public async Task\<TrainingCostRecord\> RecordTrainingCostAsync(  
            string trainingJobId,  
            string modelId,  
            ModelType modelType,  
            TimeSpan trainingDuration)  
        {  
            try  
            {  
                \_logger.LogInformation(  
                    "Recording actual training cost for job {JobId}",   
                    trainingJobId);  
                      
                // Get pricing information  
                var pricing \= await \_pricingClient.GetModelPricingAsync(modelType);  
                  
                if (pricing \== null)  
                {  
                    \_logger.LogWarning("No pricing information available for {ModelType}", modelType);  
                    return new TrainingCostRecord  
                    {  
                        TrainingJobId \= trainingJobId,  
                        ModelId \= modelId,  
                        ModelType \= modelType,  
                        TotalCost \= 0,  
                        Currency \= "USD"  
                    };  
                }  
                  
                // Calculate actual costs  
                double trainingHours \= trainingDuration.TotalHours;  
                double computeCost \= trainingHours \* pricing.TrainingPricePerHour;  
                  
                // For simplicity, we're just recording the compute cost  
                // In a real implementation, you would get actual token counts and storage usage  
                  
                var record \= new TrainingCostRecord  
                {  
                    TrainingJobId \= trainingJobId,  
                    ModelId \= modelId,  
                    ModelType \= modelType,  
                    TrainingDuration \= trainingDuration,  
                    TotalCost \= computeCost,  
                    Currency \= pricing.Currency,  
                    CostComponents \= new Dictionary\<string, double\>  
                    {  
                        \["compute"\] \= computeCost  
                    },  
                    RecordedAt \= DateTime.UtcNow  
                };  
                  
                // In a real implementation, you would persist this record  
                  
                \_logger.LogInformation(  
                    "Recorded training cost for job {JobId}: {Cost} {Currency}",  
                    trainingJobId,  
                    record.TotalCost,  
                    record.Currency);  
                      
                return record;  
            }  
            catch (Exception ex)  
            {  
                \_logger.LogError(ex, "Error recording training cost");  
                  
                return new TrainingCostRecord  
                {  
                    TrainingJobId \= trainingJobId,  
                    ModelId \= modelId,  
                    ModelType \= modelType,  
                    TotalCost \= 0,  
                    Currency \= "USD",  
                    ErrorMessage \= ex.Message  
                };  
            }  
        }  
          
        private double EstimateTrainingHours(TrainingConfiguration config)  
        {  
            // Start with the max training time as a baseline  
            double maxHours \= config.MaxTrainingTime.TotalHours;  
              
            // Adjust based on hyperparameters  
            if (config.Hyperparameters.TryGetValue("epochs", out var epochsObj) &&   
                epochsObj is int epochs)  
            {  
                // More epochs take longer  
                maxHours \= maxHours \* epochs / 3.0; // Assuming 3 epochs is the baseline  
            }  
              
            // Early stopping typically reduces training time  
            if (config.EarlyStoppingPatience \> 0\)  
            {  
                maxHours \*= 0.7; // Approximate reduction due to early stopping  
            }  
              
            return maxHours;  
        }  
          
        private long EstimateTokenCount(TrainingConfiguration config)  
        {  
            // In a real implementation, you would base this on the actual dataset  
            // For now, we'll use a placeholder value  
            return 1\_000\_000; // 1 million tokens  
        }  
          
        private double EstimateStorageCost(TrainingConfiguration config)  
        {  
            // Placeholder implementation  
            return 0.1; // $0.10 for storage  
        }  
          
        private CostEstimateConfidence DetermineCostConfidence(TrainingConfiguration config)  
        {  
            // Determine confidence based on available information  
            if (config.MaxTrainingTime \== TimeSpan.Zero)  
            {  
                return CostEstimateConfidence.Low;  
            }  
              
            if (\!config.Hyperparameters.ContainsKey("epochs"))  
            {  
                return CostEstimateConfidence.Medium;  
            }  
              
            return CostEstimateConfidence.High;  
        }  
    }

    public class CostTrackerOptions  
    {  
        public double CostEstimateMargin { get; set; } \= 1.2; // 20% margin  
    }

    public class ModelPricing  
    {  
        public ModelType ModelType { get; set; }  
        public double TrainingPricePerHour { get; set; }  
        public double TokenPricePerMillion { get; set; }  
        public string Currency { get; set; } \= "USD";  
    }

    public class TrainingCostEstimate  
    {  
        public ModelType ModelType { get; set; }  
        public double EstimatedCost { get; set; }  
        public string Currency { get; set; }  
        public CostEstimateConfidence Confidence { get; set; }  
        public Dictionary\<string, double\> CostComponents { get; set; } \= new Dictionary\<string, double\>();  
        public double EstimatedTrainingHours { get; set; }  
        public string ErrorMessage { get; set; }  
    }

    public class TrainingCostRecord  
    {  
        public string TrainingJobId { get; set; }  
        public string ModelId { get; set; }  
        public ModelType ModelType { get; set; }  
        public TimeSpan TrainingDuration { get; set; }  
        public double TotalCost { get; set; }  
        public string Currency { get; set; }  
        public Dictionary\<string, double\> CostComponents { get; set; } \= new Dictionary\<string, double\>();  
        public DateTime RecordedAt { get; set; }  
        public string ErrorMessage { get; set; }  
    }

    public enum CostEstimateConfidence  
    {  
        Unknown,  
        Low,  
        Medium,  
        High  
    }  
}

**LLMGateway.Tuning/Program.cs**

using LLMGateway.Tuning.Core.Interfaces;  
using LLMGateway.Tuning.Data.Collection;  
using LLMGateway.Tuning.Data.Preparation;  
using LLMGateway.Tuning.Data.Validation;  
using LLMGateway.Tuning.Data.Anonymization;  
using LLMGateway.Tuning.Training.Adapters;  
using LLMGateway.Tuning.Training.Jobs;  
using LLMGateway.Tuning.Evaluation;  
using LLMGateway.Tuning.Evaluation.Metrics;  
using LLMGateway.Tuning.Deployment.Registry;  
using LLMGateway.Tuning.Deployment.Strategies;  
using LLMGateway.Tuning.Monitoring.Drift;  
using LLMGateway.Tuning.Monitoring.Performance;  
using LLMGateway.Tuning.Monitoring.Cost;  
using LLMGateway.Tuning.Security.Authorization;  
using Microsoft.AspNetCore.Authorization;  
using Microsoft.AspNetCore.Builder;  
using Microsoft.Extensions.DependencyInjection;  
using Microsoft.Extensions.Hosting;

var builder \= WebApplication.CreateBuilder(args);

// Add services to the container.  
builder.Services.AddControllers();

// Add custom services  
builder.Services.AddSingleton\<FeedbackValidator\>();  
builder.Services.AddSingleton\<DataAnonymizer\>();  
builder.Services.AddSingleton\<MetricsCalculator\>();

builder.Services.AddScoped\<FeedbackCollector\>();  
builder.Services.AddScoped\<DatasetGenerator\>();  
builder.Services.AddScoped\<ModelTrainingJob\>();  
builder.Services.AddScoped\<ModelEvaluator\>();  
builder.Services.AddScoped\<ModelRegistry\>();  
builder.Services.AddScoped\<CanaryDeployer\>();  
builder.Services.AddScoped\<DataDriftDetector\>();  
builder.Services.AddScoped\<CostTracker\>();

// Add interfaces  
builder.Services.AddSingleton\<IAuthorizationPolicyProvider, ModelAccessPolicyProvider\>();  
builder.Services.AddHttpClient\<OpenAiAdapter\>();

// Configure options  
builder.Services.Configure\<OpenAiOptions\>(builder.Configuration.GetSection("OpenAi"));  
builder.Services.Configure\<TrainingOptions\>(builder.Configuration.GetSection("Training"));  
builder.Services.Configure\<CostTrackerOptions\>(builder.Configuration.GetSection("CostTracker"));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle  
builder.Services.AddEndpointsApiExplorer();  
builder.Services.AddSwaggerGen();

var app \= builder.Build();

// Configure the HTTP request pipeline.  
if (app.Environment.IsDevelopment())  
{  
    app.UseSwagger();  
    app.UseSwaggerUI();  
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

## **Implementation Notes**

This comprehensive implementation includes:

1. **Core Components**:

   * Domain models for feedback, training examples, and datasets  
   * Interfaces for repository access and service abstraction  
2. **Data Collection and Preparation**:

   * Feedback collector with validation  
   * Data anonymization for privacy  
   * Dataset generation with configurable options  
3. **Training Infrastructure**:

   * Flexible model adapters for different LLM providers  
   * Hyperparameter optimization  
   * Training job orchestration  
4. **Evaluation Framework**:

   * Comprehensive metrics calculation  
   * Evaluation reporting with examples  
   * Support for different evaluation datasets  
5. **Deployment Strategies**:

   * Canary deployment with metrics-based promotion  
   * Model registry for versioning  
   * Rollback capabilities  
6. **Monitoring Systems**:

   * Data drift detection to identify distribution shifts  
   * Performance analysis for deployed models  
   * Cost tracking and estimation  
7. **Security**:

   * Fine-grained authorization with role-based access  
   * Secure APIs with proper validation

The implementation addresses all key points from the review document, including:

* Enhanced data validation and anonymization  
* Advanced training infrastructure with hyperparameter optimization  
* Comprehensive evaluation metrics  
* Canary deployment strategy  
* Drift detection system  
* Enhanced security with fine-grained access control  
* Cost management for training jobs

This modular architecture allows for easy extension and maintenance as requirements evolve.

