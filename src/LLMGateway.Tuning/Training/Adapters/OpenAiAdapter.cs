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
        private readonly ILogger<OpenAiAdapter> _logger;
        private readonly HttpClient _httpClient;
        private readonly OpenAiOptions _options;

        public OpenAiAdapter(
            ILogger<OpenAiAdapter> logger,
            HttpClient httpClient,
            IOptions<OpenAiOptions> options)
        {
            _logger = logger;
            _httpClient = httpClient;
            _options = options.Value;
            
            // Configure HttpClient
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.ApiKey}");
            _httpClient.Timeout = TimeSpan.FromSeconds(_options.Timeout);
        }

        public async Task<string> CreateFineTuningJobAsync(string fileId, TrainingConfiguration config)
        {
            try
            {
                var requestData = new
                {
                    training_file = fileId,
                    model = config.BaseModelId ?? "gpt-3.5-turbo",
                    hyperparameters = new
                    {
                        n_epochs = config.Epochs,
                        batch_size = config.BatchSize,
                        learning_rate_multiplier = config.LearningRate
                    },
                    suffix = config.Metadata.ContainsKey("ModelSuffix") ? config.Metadata["ModelSuffix"] : null
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(requestData),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(
                    "https://api.openai.com/v1/fine_tuning/jobs",
                    content);

                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var jobResponse = JsonSerializer.Deserialize<OpenAiFineTuningResult>(
                    responseBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                _logger.LogInformation("Created OpenAI fine-tuning job with ID: {JobId}", jobResponse.Id);
                
                return jobResponse.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating OpenAI fine-tuning job");
                throw;
            }
        }

        public async Task<TrainingJobStatus> GetJobStatusAsync(string jobId)
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"https://api.openai.com/v1/fine_tuning/jobs/{jobId}");

                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var jobResponse = JsonSerializer.Deserialize<dynamic>(
                    responseBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                string status = jobResponse.status.ToString();
                string message = string.Empty;

                if (jobResponse.Property("error") != null)
                {
                    message = jobResponse.error.message.ToString();
                }

                return new TrainingJobStatus
                {
                    JobId = jobId,
                    Status = MapJobStatus(status),
                    Message = message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OpenAI fine-tuning job status");
                return new TrainingJobStatus
                {
                    JobId = jobId,
                    Status = TrainingStatus.Unknown,
                    Message = ex.Message
                };
            }
        }

        public async Task<string> PredictAsync(string modelId, string prompt)
        {
            try
            {
                var requestData = new
                {
                    model = modelId,
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 1000
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(requestData),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(
                    "https://api.openai.com/v1/chat/completions",
                    content);

                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var completionResponse = JsonSerializer.Deserialize<dynamic>(
                    responseBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return completionResponse.choices[0].message.content.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting prediction from OpenAI");
                throw;
            }
        }

        public async Task<TrainingResult> TrainModelAsync(string trainingData, string validationData, TrainingConfiguration config)
        {
            try
            {
                // Upload training file
                var trainingFileId = await UploadDatasetAsync(trainingData);
                
                // Upload validation file if provided
                string validationFileId = null;
                if (!string.IsNullOrEmpty(validationData))
                {
                    validationFileId = await UploadDatasetAsync(validationData);
                }
                
                // Create fine-tuning job
                var jobId = await CreateFineTuningJobAsync(trainingFileId, config);
                
                // Return result with job ID
                return new TrainingResult
                {
                    JobId = jobId,
                    ModelId = null, // Model ID will be available when the job completes
                    Status = TrainingStatus.Pending
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error training OpenAI model");
                return new TrainingResult
                {
                    ErrorMessage = ex.Message,
                    Status = TrainingStatus.Failed
                };
            }
        }

        public async Task<string> UploadDatasetAsync(string formattedData)
        {
            try
            {
                var content = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(formattedData));
                content.Add(fileContent, "file", "training_data.jsonl");
                content.Add(new StringContent("fine-tune"), "purpose");

                var response = await _httpClient.PostAsync(
                    "https://api.openai.com/v1/files",
                    content);

                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var fileResponse = JsonSerializer.Deserialize<dynamic>(
                    responseBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                string fileId = fileResponse.id.ToString();
                _logger.LogInformation("Uploaded dataset file with ID: {FileId}", fileId);
                
                return fileId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading dataset to OpenAI");
                throw;
            }
        }

        private TrainingStatus MapJobStatus(string openaiStatus)
        {
            return openaiStatus.ToLower() switch
            {
                "created" => TrainingStatus.Pending,
                "running" => TrainingStatus.Running,
                "succeeded" => TrainingStatus.Completed,
                "failed" => TrainingStatus.Failed,
                "cancelled" => TrainingStatus.Cancelled,
                _ => TrainingStatus.Unknown
            };
        }
    }

    public class OpenAiOptions
    {
        public string ApiKey { get; set; }
        public int Timeout { get; set; } = 300; // seconds
    }

    public class OpenAiFineTuningResult
    {
        public string Id { get; set; }
        public string Model { get; set; }
        public string Status { get; set; }
        public Dictionary<string, double> ValidationMetrics { get; set; }
    }

    public enum TrainingStatus
    {
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
        public TrainingStatus Status { get; set; }
        public string Message { get; set; }
    }

    public class TrainingResult
    {
        public string JobId { get; set; }
        public string ModelId { get; set; }
        public TrainingStatus Status { get; set; }
        public string ErrorMessage { get; set; }
    }
}
