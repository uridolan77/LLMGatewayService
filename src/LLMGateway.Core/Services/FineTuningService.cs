using LLMGateway.Core.Exceptions;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.FineTuning;
using Microsoft.Extensions.Logging;
using System.Text;

namespace LLMGateway.Core.Services;

/// <summary>
/// Service for fine-tuning operations
/// </summary>
public class FineTuningService : IFineTuningService
{
    private readonly IFineTuningRepository _repository;
    private readonly IProviderService _providerService;
    private readonly ILogger<FineTuningService> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="repository">Fine-tuning repository</param>
    /// <param name="providerService">Provider service</param>
    /// <param name="logger">Logger</param>
    public FineTuningService(
        IFineTuningRepository repository,
        IProviderService providerService,
        ILogger<FineTuningService> logger)
    {
        _repository = repository;
        _providerService = providerService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<FineTuningJob>> GetAllJobsAsync(string userId)
    {
        try
        {
            return await _repository.GetAllJobsAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all fine-tuning jobs for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<FineTuningJob> GetJobAsync(string jobId, string userId)
    {
        try
        {
            var job = await _repository.GetJobByIdAsync(jobId);
            if (job == null)
            {
                throw new NotFoundException($"Fine-tuning job with ID {jobId} not found");
            }

            // Check if the user has access to the job
            if (job.CreatedBy != userId)
            {
                throw new ForbiddenException("You don't have access to this fine-tuning job");
            }

            return job;
        }
        catch (Exception ex) when (ex is not NotFoundException && ex is not ForbiddenException)
        {
            _logger.LogError(ex, "Failed to get fine-tuning job {JobId} for user {UserId}", jobId, userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<FineTuningJob> CreateJobAsync(CreateFineTuningJobRequest request, string userId)
    {
        try
        {
            // Validate the request
            ValidateCreateJobRequest(request);

            // Get the provider
            var provider = _providerService.GetProvider(request.Provider);
            if (provider == null)
            {
                throw new ProviderNotFoundException(request.Provider);
            }

            // Check if the provider supports fine-tuning
            var fineTuningProvider = provider as IFineTuningProvider;
            if (fineTuningProvider == null || !fineTuningProvider.SupportsFineTuning)
            {
                throw new ValidationException($"Provider {request.Provider} does not support fine-tuning");
            }

            // Check if the base model is supported
            var supportedModels = await fineTuningProvider.GetSupportedBaseModelsAsync();
            if (!supportedModels.Contains(request.BaseModelId))
            {
                throw new ValidationException($"Base model {request.BaseModelId} is not supported for fine-tuning by provider {request.Provider}");
            }

            // Check if the training file exists
            var trainingFile = await _repository.GetFileByIdAsync(request.TrainingFileId);
            if (trainingFile == null)
            {
                throw new NotFoundException($"Training file with ID {request.TrainingFileId} not found");
            }

            // Check if the validation file exists (if provided)
            FineTuningFile? validationFile = null;
            if (!string.IsNullOrEmpty(request.ValidationFileId))
            {
                validationFile = await _repository.GetFileByIdAsync(request.ValidationFileId);
                if (validationFile == null)
                {
                    throw new NotFoundException($"Validation file with ID {request.ValidationFileId} not found");
                }
            }

            // Create the job
            var job = new FineTuningJob
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                Description = request.Description ?? string.Empty,
                Provider = request.Provider,
                BaseModelId = request.BaseModelId,
                TrainingFileId = request.TrainingFileId,
                ValidationFileId = request.ValidationFileId,
                Hyperparameters = request.Hyperparameters ?? new FineTuningHyperparameters(),
                Status = FineTuningJobStatus.Created,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                Tags = request.Tags ?? new List<string>()
            };

            // Create the job in the repository
            var createdJob = await _repository.CreateJobAsync(job);

            // Create the job with the provider
            try
            {
                var providerJobId = await fineTuningProvider.CreateFineTuningJobAsync(createdJob);

                // Update the job with the provider job ID
                createdJob.ProviderJobId = providerJobId;
                createdJob.Status = FineTuningJobStatus.Queued;
                createdJob = await _repository.UpdateJobAsync(createdJob);
            }
            catch (Exception ex)
            {
                // If the provider job creation fails, update the job status to failed
                createdJob.Status = FineTuningJobStatus.Failed;
                createdJob.ErrorMessage = ex.Message;
                await _repository.UpdateJobAsync(createdJob);

                _logger.LogError(ex, "Failed to create fine-tuning job with provider {Provider}", request.Provider);
                throw new ProviderException(request.Provider, $"Failed to create fine-tuning job: {ex.Message}");
            }

            return createdJob;
        }
        catch (Exception ex) when (ex is not ValidationException && ex is not NotFoundException && ex is not ProviderNotFoundException && ex is not ProviderException)
        {
            _logger.LogError(ex, "Failed to create fine-tuning job for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<FineTuningJob> CancelJobAsync(string jobId, string userId)
    {
        try
        {
            // Get the job
            var job = await GetJobAsync(jobId, userId);

            // Check if the job can be cancelled
            if (job.Status != FineTuningJobStatus.Queued && job.Status != FineTuningJobStatus.Running)
            {
                throw new ValidationException($"Fine-tuning job with status {job.Status} cannot be cancelled");
            }

            // Get the provider
            var provider = _providerService.GetProvider(job.Provider);
            if (provider == null)
            {
                throw new ProviderNotFoundException(job.Provider);
            }

            // Check if the provider supports fine-tuning
            var fineTuningProvider = provider as IFineTuningProvider;
            if (fineTuningProvider == null || !fineTuningProvider.SupportsFineTuning)
            {
                throw new ValidationException($"Provider {job.Provider} does not support fine-tuning");
            }

            // Cancel the job with the provider
            if (!string.IsNullOrEmpty(job.ProviderJobId))
            {
                try
                {
                    await fineTuningProvider.CancelFineTuningJobAsync(job.ProviderJobId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to cancel fine-tuning job {JobId} with provider {Provider}", jobId, job.Provider);
                    throw new ProviderException(job.Provider, $"Failed to cancel fine-tuning job: {ex.Message}");
                }
            }

            // Update the job status
            job.Status = FineTuningJobStatus.Cancelled;
            job.CompletedAt = DateTime.UtcNow;

            return await _repository.UpdateJobAsync(job);
        }
        catch (Exception ex) when (ex is not ValidationException && ex is not NotFoundException && ex is not ForbiddenException && ex is not ProviderNotFoundException && ex is not ProviderException)
        {
            _logger.LogError(ex, "Failed to cancel fine-tuning job {JobId} for user {UserId}", jobId, userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteJobAsync(string jobId, string userId)
    {
        try
        {
            // Get the job
            var job = await GetJobAsync(jobId, userId);

            // Delete the fine-tuned model if it exists
            if (!string.IsNullOrEmpty(job.FineTunedModelId))
            {
                var provider = _providerService.GetProvider(job.Provider);
                if (provider != null)
                {
                    var fineTuningProvider = provider as IFineTuningProvider;
                    if (fineTuningProvider != null && fineTuningProvider.SupportsFineTuning)
                    {
                        try
                        {
                            await fineTuningProvider.DeleteFineTunedModelAsync(job.FineTunedModelId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to delete fine-tuned model {ModelId} with provider {Provider}", job.FineTunedModelId, job.Provider);
                            // Continue with job deletion even if model deletion fails
                        }
                    }
                }
            }

            // Delete the job
            await _repository.DeleteJobAsync(jobId);
        }
        catch (Exception ex) when (ex is not NotFoundException && ex is not ForbiddenException)
        {
            _logger.LogError(ex, "Failed to delete fine-tuning job {JobId} for user {UserId}", jobId, userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<FineTuningStepMetric>> GetJobEventsAsync(string jobId, string userId)
    {
        try
        {
            // Get the job
            var job = await GetJobAsync(jobId, userId);

            return await _repository.GetJobEventsAsync(jobId);
        }
        catch (Exception ex) when (ex is not NotFoundException && ex is not ForbiddenException)
        {
            _logger.LogError(ex, "Failed to get events for fine-tuning job {JobId} for user {UserId}", jobId, userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<FineTuningJobSearchResponse> SearchJobsAsync(FineTuningJobSearchRequest request, string userId)
    {
        try
        {
            var (jobs, totalCount) = await _repository.SearchJobsAsync(
                userId,
                request.Query,
                request.Status,
                request.Provider,
                request.BaseModelId,
                request.Tags,
                request.CreatedBy,
                request.Page,
                request.PageSize);

            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

            return new FineTuningJobSearchResponse
            {
                Jobs = jobs.ToList(),
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = totalPages
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search fine-tuning jobs for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<FineTuningFile>> GetAllFilesAsync(string userId)
    {
        try
        {
            return await _repository.GetAllFilesAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all fine-tuning files for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<FineTuningFile> GetFileAsync(string fileId, string userId)
    {
        try
        {
            var file = await _repository.GetFileByIdAsync(fileId);
            if (file == null)
            {
                throw new NotFoundException($"Fine-tuning file with ID {fileId} not found");
            }

            // Check if the user has access to the file
            if (file.CreatedBy != userId)
            {
                throw new ForbiddenException("You don't have access to this fine-tuning file");
            }

            return file;
        }
        catch (Exception ex) when (ex is not NotFoundException && ex is not ForbiddenException)
        {
            _logger.LogError(ex, "Failed to get fine-tuning file {FileId} for user {UserId}", fileId, userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<FineTuningFile> UploadFileAsync(UploadFineTuningFileRequest request, string userId)
    {
        try
        {
            // Validate the request
            ValidateUploadFileRequest(request);

            // Get the provider
            var provider = _providerService.GetProvider(request.Provider);
            if (provider == null)
            {
                throw new ProviderNotFoundException(request.Provider);
            }

            // Check if the provider supports fine-tuning
            var fineTuningProvider = provider as IFineTuningProvider;
            if (fineTuningProvider == null || !fineTuningProvider.SupportsFineTuning)
            {
                throw new ValidationException($"Provider {request.Provider} does not support fine-tuning");
            }

            // Decode the file content
            string fileContent;
            try
            {
                var contentBytes = Convert.FromBase64String(request.FileContent);
                fileContent = Encoding.UTF8.GetString(contentBytes);
            }
            catch (Exception ex)
            {
                throw new ValidationException($"Invalid file content: {ex.Message}");
            }

            // Create the file
            var file = new FineTuningFile
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                Size = Encoding.UTF8.GetByteCount(fileContent),
                Purpose = request.Purpose,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                Provider = request.Provider,
                Status = "uploading"
            };

            // Create the file in the repository
            var createdFile = await _repository.CreateFileAsync(file);

            // Save the file content
            await _repository.SaveFileContentAsync(createdFile.Id, fileContent);

            // Upload the file to the provider
            try
            {
                var providerFileId = await fineTuningProvider.UploadFineTuningFileAsync(
                    createdFile.Name,
                    createdFile.Purpose,
                    fileContent);

                // Update the file with the provider file ID
                createdFile.ProviderFileId = providerFileId;
                createdFile.Status = "uploaded";
                createdFile = await _repository.UpdateFileAsync(createdFile);
            }
            catch (Exception ex)
            {
                // If the provider file upload fails, update the file status to failed
                createdFile.Status = "failed";
                await _repository.UpdateFileAsync(createdFile);

                _logger.LogError(ex, "Failed to upload fine-tuning file to provider {Provider}", request.Provider);
                throw new ProviderException(request.Provider, $"Failed to upload fine-tuning file: {ex.Message}");
            }

            return createdFile;
        }
        catch (Exception ex) when (ex is not ValidationException && ex is not ProviderNotFoundException && ex is not ProviderException)
        {
            _logger.LogError(ex, "Failed to upload fine-tuning file for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteFileAsync(string fileId, string userId)
    {
        try
        {
            // Get the file
            var file = await GetFileAsync(fileId, userId);

            // Delete the file from the provider
            if (!string.IsNullOrEmpty(file.ProviderFileId))
            {
                var provider = _providerService.GetProvider(file.Provider);
                if (provider != null)
                {
                    var fineTuningProvider = provider as IFineTuningProvider;
                    if (fineTuningProvider != null && fineTuningProvider.SupportsFineTuning)
                    {
                        try
                        {
                            await fineTuningProvider.DeleteFineTuningFileAsync(file.ProviderFileId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to delete fine-tuning file {FileId} with provider {Provider}", file.ProviderFileId, file.Provider);
                            // Continue with file deletion even if provider deletion fails
                        }
                    }
                }
            }

            // Delete the file
            await _repository.DeleteFileAsync(fileId);
        }
        catch (Exception ex) when (ex is not NotFoundException && ex is not ForbiddenException)
        {
            _logger.LogError(ex, "Failed to delete fine-tuning file {FileId} for user {UserId}", fileId, userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string> GetFileContentAsync(string fileId, string userId)
    {
        try
        {
            // Get the file
            var file = await GetFileAsync(fileId, userId);

            // Get the file content
            var content = await _repository.GetFileContentAsync(fileId);
            if (content == null)
            {
                throw new NotFoundException($"Content for fine-tuning file with ID {fileId} not found");
            }

            return content;
        }
        catch (Exception ex) when (ex is not NotFoundException && ex is not ForbiddenException)
        {
            _logger.LogError(ex, "Failed to get content for fine-tuning file {FileId} for user {UserId}", fileId, userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<FineTuningJob> SyncJobStatusAsync(string jobId, string userId)
    {
        try
        {
            // Get the job
            var job = await GetJobAsync(jobId, userId);

            // Check if the job has a provider job ID
            if (string.IsNullOrEmpty(job.ProviderJobId))
            {
                return job;
            }

            // Get the provider
            var provider = _providerService.GetProvider(job.Provider);
            if (provider == null)
            {
                throw new ProviderNotFoundException(job.Provider);
            }

            // Check if the provider supports fine-tuning
            var fineTuningProvider = provider as IFineTuningProvider;
            if (fineTuningProvider == null || !fineTuningProvider.SupportsFineTuning)
            {
                throw new ValidationException($"Provider {job.Provider} does not support fine-tuning");
            }

            // Get the job status from the provider
            try
            {
                var (status, fineTunedModelId, errorMessage, metrics) = await fineTuningProvider.GetFineTuningJobAsync(job.ProviderJobId);

                // Update the job
                job.Status = status;
                job.FineTunedModelId = fineTunedModelId ?? job.FineTunedModelId;
                job.ErrorMessage = errorMessage ?? job.ErrorMessage;
                job.Metrics = metrics ?? job.Metrics;

                // Update completion time if the job is completed
                if (status == FineTuningJobStatus.Succeeded || status == FineTuningJobStatus.Failed || status == FineTuningJobStatus.Cancelled)
                {
                    job.CompletedAt = DateTime.UtcNow;
                }

                // Update start time if the job is running and doesn't have a start time
                if (status == FineTuningJobStatus.Running && job.StartedAt == null)
                {
                    job.StartedAt = DateTime.UtcNow;
                }

                // Update the job
                job = await _repository.UpdateJobAsync(job);

                // Get the job events
                if (status == FineTuningJobStatus.Running || status == FineTuningJobStatus.Succeeded)
                {
                    var events = await fineTuningProvider.GetFineTuningJobEventsAsync(job.ProviderJobId);
                    foreach (var @event in events)
                    {
                        await _repository.AddJobEventAsync(job.Id, @event);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync fine-tuning job {JobId} with provider {Provider}", jobId, job.Provider);
                // Don't throw an exception, just return the job as is
            }

            return job;
        }
        catch (Exception ex) when (ex is not NotFoundException && ex is not ForbiddenException && ex is not ProviderNotFoundException && ex is not ValidationException)
        {
            _logger.LogError(ex, "Failed to sync fine-tuning job {JobId} for user {UserId}", jobId, userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task SyncAllJobsStatusAsync()
    {
        try
        {
            // Get all jobs that are in progress
            var jobs = await _repository.GetJobsByStatusAsync(FineTuningJobStatus.Queued);
            jobs = jobs.Concat(await _repository.GetJobsByStatusAsync(FineTuningJobStatus.Running));

            // Sync each job
            foreach (var job in jobs)
            {
                try
                {
                    // Check if the job has a provider job ID
                    if (string.IsNullOrEmpty(job.ProviderJobId))
                    {
                        continue;
                    }

                    // Get the provider
                    var provider = _providerService.GetProvider(job.Provider);
                    if (provider == null)
                    {
                        continue;
                    }

                    // Check if the provider supports fine-tuning
                    var fineTuningProvider = provider as IFineTuningProvider;
                    if (fineTuningProvider == null || !fineTuningProvider.SupportsFineTuning)
                    {
                        continue;
                    }

                    // Get the job status from the provider
                    var (status, fineTunedModelId, errorMessage, metrics) = await fineTuningProvider.GetFineTuningJobAsync(job.ProviderJobId);

                    // Update the job
                    job.Status = status;
                    job.FineTunedModelId = fineTunedModelId ?? job.FineTunedModelId;
                    job.ErrorMessage = errorMessage ?? job.ErrorMessage;
                    job.Metrics = metrics ?? job.Metrics;

                    // Update completion time if the job is completed
                    if (status == FineTuningJobStatus.Succeeded || status == FineTuningJobStatus.Failed || status == FineTuningJobStatus.Cancelled)
                    {
                        job.CompletedAt = DateTime.UtcNow;
                    }

                    // Update start time if the job is running and doesn't have a start time
                    if (status == FineTuningJobStatus.Running && job.StartedAt == null)
                    {
                        job.StartedAt = DateTime.UtcNow;
                    }

                    // Update the job
                    await _repository.UpdateJobAsync(job);

                    // Get the job events
                    if (status == FineTuningJobStatus.Running || status == FineTuningJobStatus.Succeeded)
                    {
                        var events = await fineTuningProvider.GetFineTuningJobEventsAsync(job.ProviderJobId);
                        foreach (var @event in events)
                        {
                            await _repository.AddJobEventAsync(job.Id, @event);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to sync fine-tuning job {JobId} with provider {Provider}", job.Id, job.Provider);
                    // Continue with the next job
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync all fine-tuning jobs");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<FineTuningAnalytics> GetAnalyticsAsync(string userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            _logger.LogInformation("Getting fine-tuning analytics for user {UserId}", userId);

            // This would typically query comprehensive analytics from the database
            // For now, return simulated analytics
            var analytics = new FineTuningAnalytics
            {
                TotalJobs = 25,
                SuccessfulJobs = 20,
                FailedJobs = 3,
                CancelledJobs = 2,
                SuccessRate = 80.0,
                TotalCost = 1250.75m,
                AverageCostPerJob = 50.03m,
                TotalTrainingTime = TimeSpan.FromHours(48),
                AverageTrainingTime = TimeSpan.FromHours(1.92),
                PopularBaseModels = new List<Models.FineTuning.ModelUsageStats>
                {
                    new() { ModelId = "gpt-3.5-turbo", Provider = "OpenAI", RequestCount = 15, Cost = 750.45m },
                    new() { ModelId = "claude-3-haiku", Provider = "Anthropic", RequestCount = 8, Cost = 400.20m },
                    new() { ModelId = "command", Provider = "Cohere", RequestCount = 2, Cost = 100.10m }
                },
                CostTrends = new List<CostTrend>
                {
                    new() { Date = DateTime.UtcNow.AddDays(-30), TotalCost = 800.00m, JobCount = 5 },
                    new() { Date = DateTime.UtcNow.AddDays(-15), TotalCost = 1000.00m, JobCount = 8 },
                    new() { Date = DateTime.UtcNow, TotalCost = 1250.75m, JobCount = 12 }
                },
                PerformanceMetrics = new FineTuningPerformanceMetrics
                {
                    AverageAccuracy = 0.92,
                    AverageLoss = 0.15,
                    AverageF1Score = 0.89,
                    BestPerformingJob = "job-123",
                    WorstPerformingJob = "job-456"
                }
            };

            return analytics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get fine-tuning analytics for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<FineTuningCostBreakdown> GetCostBreakdownAsync(string userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            _logger.LogInformation("Getting fine-tuning cost breakdown for user {UserId}", userId);

            // This would typically query cost data from the database
            // For now, return simulated breakdown
            var breakdown = new FineTuningCostBreakdown
            {
                TotalCost = 1250.75m,
                Currency = "USD",
                BreakdownByProvider = new Dictionary<string, decimal>
                {
                    ["OpenAI"] = 750.45m,
                    ["Anthropic"] = 350.20m,
                    ["Cohere"] = 150.10m
                },
                BreakdownByModel = new Dictionary<string, decimal>
                {
                    ["gpt-3.5-turbo"] = 750.45m,
                    ["claude-3-haiku"] = 350.20m,
                    ["command"] = 150.10m
                },
                BreakdownByJobType = new Dictionary<string, decimal>
                {
                    ["classification"] = 600.30m,
                    ["generation"] = 450.25m,
                    ["summarization"] = 200.20m
                },
                TrainingCosts = 1000.60m,
                ValidationCosts = 150.09m,
                StorageCosts = 100.06m,
                CostTrends = new List<Models.FineTuning.CostTrendPoint>
                {
                    new() { Date = DateTime.UtcNow.AddDays(-30), Cost = 800.00m },
                    new() { Date = DateTime.UtcNow.AddDays(-15), Cost = 1000.00m },
                    new() { Date = DateTime.UtcNow, Cost = 1250.75m }
                }
            };

            return breakdown;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get fine-tuning cost breakdown for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<FineTuningCostEstimate> EstimateCostAsync(EstimateFineTuningCostRequest request, string userId)
    {
        try
        {
            _logger.LogInformation("Estimating fine-tuning cost for user {UserId}", userId);

            // This would typically use provider-specific pricing models
            // For now, return simulated estimate
            var estimate = new FineTuningCostEstimate
            {
                EstimatedTotalCost = 125.50m,
                Currency = request.Currency,
                TrainingCost = 100.40m,
                DataPreparationCost = 15.05m,
                ValidationCost = 7.03m,
                StorageCost = 3.02m,
                CostBreakdown = new Dictionary<string, decimal>
                {
                    ["training_tokens"] = 100.40m,
                    ["data_preparation"] = 15.05m,
                    ["validation"] = 7.03m,
                    ["storage"] = 3.02m
                },
                EstimatedTrainingTime = TimeSpan.FromHours(2.5),
                ConfidenceLevel = 0.85,
                CostFactors = new List<string>
                {
                    "Training data size",
                    "Model complexity",
                    "Number of epochs",
                    "Provider pricing"
                },
                OptimizationSuggestions = new List<string>
                {
                    "Consider reducing training data size for initial testing",
                    "Use fewer epochs for faster iteration",
                    "Compare pricing across providers"
                },
                GeneratedAt = DateTime.UtcNow
            };

            return estimate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to estimate fine-tuning cost for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ModelPerformanceComparison> CompareModelPerformanceAsync(string baseModelId, string fineTunedModelId, string userId)
    {
        try
        {
            _logger.LogInformation("Comparing model performance for user {UserId}", userId);

            // This would typically run actual performance tests
            // For now, return simulated comparison
            var comparison = new ModelPerformanceComparison
            {
                BaseModelId = baseModelId,
                FineTunedModelId = fineTunedModelId,
                Metrics = new Dictionary<string, PerformanceMetric>
                {
                    ["accuracy"] = new()
                    {
                        MetricName = "Accuracy",
                        BaseModelValue = 0.75,
                        FineTunedModelValue = 0.92,
                        ImprovementPercentage = 22.7,
                        IsImprovement = true,
                        Unit = "percentage"
                    },
                    ["f1_score"] = new()
                    {
                        MetricName = "F1 Score",
                        BaseModelValue = 0.72,
                        FineTunedModelValue = 0.89,
                        ImprovementPercentage = 23.6,
                        IsImprovement = true,
                        Unit = "score"
                    }
                },
                OverallImprovementScore = 23.15,
                Summary = new PerformanceComparisonSummary
                {
                    TotalMetricsCompared = 2,
                    MetricsImproved = 2,
                    MetricsDegraded = 0,
                    MetricsUnchanged = 0,
                    AverageImprovement = 23.15,
                    BestPerformingMetric = "f1_score",
                    WorstPerformingMetric = null
                },
                Recommendations = new List<string>
                {
                    "Fine-tuned model shows significant improvement",
                    "Consider deploying to production",
                    "Monitor performance in real-world scenarios"
                },
                GeneratedAt = DateTime.UtcNow
            };

            return comparison;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compare model performance for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<FineTuningRecommendations> GetRecommendationsAsync(string userId, string useCase)
    {
        try
        {
            _logger.LogInformation("Getting fine-tuning recommendations for user {UserId}", userId);

            // This would typically analyze user's data and usage patterns
            // For now, return simulated recommendations
            var recommendations = new FineTuningRecommendations
            {
                ModelRecommendations = new List<ModelRecommendation>
                {
                    new()
                    {
                        RecommendedModel = "gpt-3.5-turbo",
                        Reason = "Best balance of performance and cost for text classification",
                        ConfidenceScore = 0.85,
                        ExpectedPerformanceImprovement = 15.5,
                        EstimatedCost = 125.50m,
                        UseCases = new List<string> { "text classification", "sentiment analysis" }
                    }
                },
                DataRecommendations = new List<DataRecommendation>
                {
                    new()
                    {
                        RecommendationType = "data_quality",
                        Description = "Increase training data diversity",
                        Priority = "high",
                        ExpectedImpact = "Improved model generalization",
                        ImplementationSteps = new List<string>
                        {
                            "Collect data from different domains",
                            "Balance class distribution",
                            "Add data augmentation"
                        }
                    }
                },
                TrainingRecommendations = new List<TrainingRecommendation>
                {
                    new()
                    {
                        ParameterName = "learning_rate",
                        RecommendedValue = 0.0001,
                        CurrentValue = 0.001,
                        Reason = "Lower learning rate for better convergence",
                        ImpactLevel = "medium",
                        ConfidenceLevel = 0.8
                    }
                },
                OverallRecommendationScore = 85.5,
                PriorityRecommendations = new List<string>
                {
                    "Increase training data diversity",
                    "Use lower learning rate",
                    "Consider gpt-3.5-turbo for cost efficiency"
                },
                GeneratedAt = DateTime.UtcNow
            };

            return recommendations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get fine-tuning recommendations for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<DataQualityReport> ValidateDataQualityAsync(string fileId, string userId)
    {
        try
        {
            _logger.LogInformation("Validating data quality for file {FileId} for user {UserId}", fileId, userId);

            // Get the file
            var file = await GetFileAsync(fileId, userId);

            // This would typically analyze the actual file content
            // For now, return simulated quality report
            var report = new DataQualityReport
            {
                FileId = fileId,
                OverallQualityScore = 85.5,
                QualityStatus = "good",
                TotalRecords = 1000,
                ValidRecords = 855,
                InvalidRecords = 145,
                QualityIssues = new List<DataQualityIssue>
                {
                    new()
                    {
                        IssueType = "missing_fields",
                        Severity = "medium",
                        Description = "Some records are missing required fields",
                        AffectedRecordsCount = 85,
                        SuggestedFix = "Fill missing fields or remove incomplete records",
                        AutoFixable = false
                    },
                    new()
                    {
                        IssueType = "duplicate_records",
                        Severity = "low",
                        Description = "Duplicate records found",
                        AffectedRecordsCount = 60,
                        SuggestedFix = "Remove duplicate records",
                        AutoFixable = true
                    }
                },
                DataStatistics = new DataStatistics
                {
                    AverageInputLength = 125.5,
                    AverageOutputLength = 45.2,
                    UniqueInputs = 950,
                    DuplicateRecords = 60,
                    LanguageDistribution = new Dictionary<string, int>
                    {
                        ["en"] = 800,
                        ["es"] = 150,
                        ["fr"] = 50
                    }
                },
                Recommendations = new List<DataQualityRecommendation>
                {
                    new()
                    {
                        RecommendationType = "data_cleaning",
                        Title = "Remove duplicate records",
                        Description = "Remove 60 duplicate records to improve training quality",
                        Priority = "medium",
                        ExpectedImpact = "Improved model performance",
                        EffortRequired = "low"
                    }
                },
                ValidationRulesApplied = new List<string>
                {
                    "Required fields validation",
                    "Duplicate detection",
                    "Format validation",
                    "Language detection"
                },
                GeneratedAt = DateTime.UtcNow
            };

            return report;
        }
        catch (Exception ex) when (ex is not NotFoundException && ex is not ForbiddenException)
        {
            _logger.LogError(ex, "Failed to validate data quality for file {FileId} for user {UserId}", fileId, userId);
            throw;
        }
    }



    /// <inheritdoc/>
    public async Task<IEnumerable<FineTuningTemplate>> GetTemplatesAsync(string provider, string useCase)
    {
        try
        {
            _logger.LogInformation("Getting fine-tuning templates for provider {Provider} and use case {UseCase}", provider, useCase);

            // This would typically query a template database
            // For now, return simulated templates
            var templates = new List<FineTuningTemplate>
            {
                new()
                {
                    Id = "template-1",
                    Name = "Text Classification Template",
                    Description = "Template for text classification tasks",
                    Category = "classification",
                    UseCase = "text_classification",
                    BaseModel = "gpt-3.5-turbo",
                    RecommendedProvider = provider,
                    TrainingParameters = new Dictionary<string, object>
                    {
                        ["learning_rate"] = 0.0001,
                        ["batch_size"] = 16,
                        ["epochs"] = 3
                    },
                    DataRequirements = new DataRequirements
                    {
                        MinimumExamples = 100,
                        RecommendedExamples = 1000,
                        DataFormat = "jsonl",
                        RequiredFields = new List<string> { "prompt", "completion" }
                    },
                    EstimatedCostRange = new CostRange
                    {
                        MinimumCost = 50.00m,
                        MaximumCost = 200.00m,
                        AverageCost = 125.00m,
                        Currency = "USD"
                    },
                    EstimatedTrainingTime = TimeSpan.FromHours(2),
                    DifficultyLevel = "beginner",
                    Tags = new List<string> { "classification", "text", "beginner" },
                    UsageCount = 150,
                    SuccessRate = 0.85,
                    CreatedAt = DateTime.UtcNow.AddDays(-30),
                    UpdatedAt = DateTime.UtcNow.AddDays(-5)
                }
            };

            return templates.Where(t => string.IsNullOrEmpty(provider) || t.RecommendedProvider == provider)
                          .Where(t => string.IsNullOrEmpty(useCase) || t.UseCase == useCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get fine-tuning templates for provider {Provider} and use case {UseCase}", provider, useCase);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<FineTuningJob> CreateJobFromTemplateAsync(string templateId, CreateJobFromTemplateRequest request, string userId)
    {
        try
        {
            _logger.LogInformation("Creating fine-tuning job from template {TemplateId} for user {UserId}", templateId, userId);

            // This would typically load the template from database
            // For now, create a simulated template-based job
            var createJobRequest = new CreateFineTuningJobRequest
            {
                Name = request.JobName,
                Provider = "OpenAI", // Would come from template
                BaseModelId = "gpt-3.5-turbo", // Would come from template
                TrainingFileId = request.TrainingFileId,
                ValidationFileId = request.ValidationFileId,
                Hyperparameters = new FineTuningHyperparameters
                {
                    LearningRate = (float?)0.0001, // Would come from template
                    BatchSize = 16, // Would come from template
                    Epochs = 3 // Would come from template
                },
                Tags = request.Tags ?? new List<string>()
            };

            // Apply parameter overrides
            if (request.ParameterOverrides != null)
            {
                foreach (var (key, value) in request.ParameterOverrides)
                {
                    switch (key.ToLower())
                    {
                        case "learning_rate":
                            if (double.TryParse(value.ToString(), out var lr))
                                createJobRequest.Hyperparameters.LearningRate = (float?)lr;
                            break;
                        case "batch_size":
                            if (int.TryParse(value.ToString(), out var bs))
                                createJobRequest.Hyperparameters.BatchSize = bs;
                            break;
                        case "epochs":
                            if (int.TryParse(value.ToString(), out var ep))
                                createJobRequest.Hyperparameters.Epochs = ep;
                            break;
                    }
                }
            }

            // Create the job using the existing method
            var job = await CreateJobAsync(createJobRequest, userId);

            // Add template reference
            job.Tags.Add($"template:{templateId}");
            job = await _repository.UpdateJobAsync(job);

            return job;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create fine-tuning job from template {TemplateId} for user {UserId}", templateId, userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<FineTuningJobInsights> GetJobInsightsAsync(string jobId, string userId)
    {
        try
        {
            _logger.LogInformation("Getting fine-tuning job insights for job {JobId} for user {UserId}", jobId, userId);

            // Get the job
            var job = await GetJobAsync(jobId, userId);

            // This would typically analyze job performance and generate insights
            // For now, return simulated insights
            var insights = new FineTuningJobInsights
            {
                JobId = jobId,
                PerformanceInsights = new PerformanceInsights
                {
                    OverallPerformanceScore = 85.5,
                    PerformanceVsBaseline = 15.2,
                    BestPerformingEpochs = new List<int> { 2, 3 },
                    PerformanceBottlenecks = new List<string>
                    {
                        "Learning rate too high in early epochs",
                        "Data imbalance affecting convergence"
                    }
                },
                TrainingInsights = new TrainingInsights
                {
                    TrainingEfficiencyScore = 78.3,
                    ConvergenceAnalysis = new ConvergenceAnalysis
                    {
                        Converged = true,
                        ConvergenceEpoch = 2,
                        ConvergenceRate = 0.85,
                        EarlyStoppingTriggered = false
                    },
                    TrainingStability = 0.92
                },
                CostInsights = new CostInsights
                {
                    TotalCost = 125.50m,
                    CostEfficiencyScore = 82.1,
                    CostVsBudget = 0.00m, // No budget set
                    CostOptimizationOpportunities = new List<string>
                    {
                        "Consider using fewer epochs for similar results",
                        "Optimize batch size for better cost efficiency"
                    }
                },
                DataInsights = new DataInsights
                {
                    DataQualityScore = 88.7,
                    DataUtilizationEfficiency = 0.91,
                    DataDiversityScore = 0.76,
                    DataRecommendations = new List<string>
                    {
                        "Increase data diversity for better generalization",
                        "Balance class distribution"
                    }
                },
                ModelInsights = new ModelInsights
                {
                    ModelComplexityScore = 0.65,
                    ModelGeneralizationAbility = 0.83,
                    ModelRobustnessScore = 0.79,
                    ModelInterpretability = 0.42
                },
                KeyFindings = new List<string>
                {
                    "Model achieved 85.5% performance score",
                    "Training converged efficiently in 2 epochs",
                    "Cost efficiency could be improved by 15%"
                },
                SuccessFactors = new List<string>
                {
                    "High-quality training data",
                    "Appropriate learning rate",
                    "Good model-task alignment"
                },
                AreasForImprovement = new List<string>
                {
                    "Data diversity",
                    "Cost optimization",
                    "Training efficiency"
                },
                GeneratedAt = DateTime.UtcNow
            };

            return insights;
        }
        catch (Exception ex) when (ex is not NotFoundException && ex is not ForbiddenException)
        {
            _logger.LogError(ex, "Failed to get fine-tuning job insights for job {JobId} for user {UserId}", jobId, userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Models.Analytics.ExportData> ExportJobDataAsync(string jobId, Models.Analytics.ExportFormat format, string userId)
    {
        try
        {
            _logger.LogInformation("Exporting fine-tuning job data for job {JobId} for user {UserId}", jobId, userId);

            // Get the job
            var job = await GetJobAsync(jobId, userId);

            // This would typically generate and export the actual job data
            // For now, return simulated export info
            var exportData = new Models.Analytics.ExportData
            {
                Id = Guid.NewGuid().ToString(),
                FileName = $"finetuning-job-{jobId}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.{format.ToString().ToLower()}",
                DownloadUrl = $"/api/exports/{Guid.NewGuid()}",
                FileSizeBytes = 1024 * 100, // 100KB
                Format = format,
                GeneratedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                RecordCount = 500,
                Status = "completed",
                ProgressPercentage = 100.0
            };

            return exportData;
        }
        catch (Exception ex) when (ex is not NotFoundException && ex is not ForbiddenException)
        {
            _logger.LogError(ex, "Failed to export fine-tuning job data for job {JobId} for user {UserId}", jobId, userId);
            throw;
        }
    }

    #region Helper methods

    private static void ValidateCreateJobRequest(CreateFineTuningJobRequest request)
    {
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.Add("Name", "Job name is required");
        }

        if (string.IsNullOrWhiteSpace(request.Provider))
        {
            errors.Add("Provider", "Provider is required");
        }

        if (string.IsNullOrWhiteSpace(request.BaseModelId))
        {
            errors.Add("BaseModelId", "Base model ID is required");
        }

        if (string.IsNullOrWhiteSpace(request.TrainingFileId))
        {
            errors.Add("TrainingFileId", "Training file ID is required");
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("Invalid fine-tuning job request", errors);
        }
    }

    private static void ValidateUploadFileRequest(UploadFineTuningFileRequest request)
    {
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.Add("Name", "File name is required");
        }

        if (string.IsNullOrWhiteSpace(request.Provider))
        {
            errors.Add("Provider", "Provider is required");
        }

        if (string.IsNullOrWhiteSpace(request.FileContent))
        {
            errors.Add("FileContent", "File content is required");
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("Invalid fine-tuning file request", errors);
        }
    }

    #endregion
}
