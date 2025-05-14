using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LLMGateway.Tuning.Core.Enums;
using LLMGateway.Tuning.Core.Interfaces;
using LLMGateway.Tuning.Core.Models;
using LLMGateway.Tuning.Deployment.Registry;
using LLMGateway.Tuning.Training.Adapters;
using LLMGateway.Tuning.Training.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LLMGateway.Tuning.Training.Jobs
{
    public class ModelTrainingJob
    {
        private readonly ILogger<ModelTrainingJob> _logger;
        private readonly IModelAdapter _modelAdapter;
        private readonly IDatasetStorage _datasetStorage;
        private readonly IModelRegistry _modelRegistry;
        private readonly TrainingOptions _options;

        public ModelTrainingJob(
            ILogger<ModelTrainingJob> logger,
            IModelAdapter modelAdapter,
            IDatasetStorage datasetStorage,
            IModelRegistry modelRegistry,
            IOptions<TrainingOptions> options)
        {
            _logger = logger;
            _modelAdapter = modelAdapter;
            _datasetStorage = datasetStorage;
            _modelRegistry = modelRegistry;
            _options = options.Value;
        }

        public async Task<ModelTrainingJobResponse> SubmitTrainingJobAsync(ModelTrainingJobRequest request)
        {
            try
            {
                _logger.LogInformation("Submitting training job for model type: {ModelType}", request.ModelType);

                // Create training configuration
                var config = new TrainingConfiguration
                {
                    ModelType = request.ModelType,
                    BaseModelId = request.BaseModelId,
                    Epochs = request.Epochs ?? _options.DefaultEpochs,
                    BatchSize = request.BatchSize ?? _options.DefaultBatchSize,
                    LearningRate = request.LearningRate ?? _options.DefaultLearningRate,
                    EarlyStoppingEnabled = request.EarlyStoppingEnabled ?? true,
                    EarlyStoppingPatience = request.EarlyStoppingPatience ?? _options.DefaultEarlyStoppingPatience,
                    Metadata = new Dictionary<string, string>
                    {
                        { "CreatedBy", request.CreatedBy },
                        { "RequestedAt", DateTime.UtcNow.ToString("o") },
                        { "DatasetId", request.DatasetId }
                    }
                };

                // Get dataset content
                var trainingData = await _datasetStorage.LoadDatasetAsync(request.DatasetId, "training");
                var validationData = await _datasetStorage.LoadDatasetAsync(request.DatasetId, "validation");

                if (string.IsNullOrEmpty(trainingData))
                {
                    _logger.LogError("Training data not found for dataset: {DatasetId}", request.DatasetId);
                    return new ModelTrainingJobResponse
                    {
                        Success = false,
                        ErrorMessage = $"Training data not found for dataset: {request.DatasetId}"
                    };
                }

                // Submit training job to the model provider
                var result = await _modelAdapter.TrainModelAsync(trainingData, validationData, config);

                if (result.Status == TrainingStatus.Failed)
                {
                    _logger.LogError("Failed to submit training job: {Error}", result.ErrorMessage);
                    return new ModelTrainingJobResponse
                    {
                        Success = false,
                        ErrorMessage = result.ErrorMessage
                    };
                }

                // Register the model in the registry
                var model = new ModelVersion
                {
                    Name = request.ModelName ?? $"{request.ModelType}-Finetuned-{DateTime.UtcNow:yyyyMMdd}",
                    BaseModelId = request.BaseModelId,
                    CreatedBy = request.CreatedBy,
                    Status = ModelStatus.Training,
                    Metadata = new Dictionary<string, string>
                    {
                        { "JobId", result.JobId },
                        { "DatasetId", request.DatasetId },
                        { "Epochs", config.Epochs.ToString() }
                    }
                };

                var modelId = await _modelRegistry.RegisterModelAsync(model);

                _logger.LogInformation("Training job submitted successfully. Model ID: {ModelId}, Job ID: {JobId}",
                    modelId, result.JobId);

                return new ModelTrainingJobResponse
                {
                    Success = true,
                    ModelId = modelId,
                    JobId = result.JobId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting training job");
                return new ModelTrainingJobResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<TrainingJobStatus> GetJobStatusAsync(string jobId)
        {
            try
            {
                _logger.LogInformation("Checking status of training job: {JobId}", jobId);
                
                var status = await _modelAdapter.GetJobStatusAsync(jobId);
                
                _logger.LogInformation("Job {JobId} status: {Status}", jobId, status.Status);
                
                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking job status");
                return new TrainingJobStatus
                {
                    JobId = jobId,
                    Status = TrainingStatus.Unknown,
                    Message = $"Error checking status: {ex.Message}"
                };
            }
        }
    }

    public class TrainingOptions
    {
        public int DefaultEpochs { get; set; } = 3;
        public int DefaultEarlyStoppingPatience { get; set; } = 3;
        public int DefaultBatchSize { get; set; } = 4;
        public double DefaultLearningRate { get; set; } = 0.0001;
    }

    public class ModelTrainingJobRequest
    {
        public ModelType ModelType { get; set; }
        public string BaseModelId { get; set; }
        public string DatasetId { get; set; }
        public string ModelName { get; set; }
        public string CreatedBy { get; set; }
        public int? Epochs { get; set; }
        public int? BatchSize { get; set; }
        public double? LearningRate { get; set; }
        public bool? EarlyStoppingEnabled { get; set; }
        public int? EarlyStoppingPatience { get; set; }
    }

    public class ModelTrainingJobResponse
    {
        public bool Success { get; set; }
        public string ModelId { get; set; }
        public string JobId { get; set; }
        public string ErrorMessage { get; set; }
    }
}
