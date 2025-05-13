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
