using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.FineTuning;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace LLMGateway.Core.Services;

/// <summary>
/// In-memory implementation of IFineTuningRepository for when database is not available
/// </summary>
public class InMemoryFineTuningRepository : IFineTuningRepository
{
    private readonly ILogger<InMemoryFineTuningRepository> _logger;
    private readonly ConcurrentDictionary<string, FineTuningJob> _jobs = new();
    private readonly ConcurrentDictionary<string, FineTuningFile> _files = new();
    private readonly ConcurrentDictionary<string, List<FineTuningStepMetric>> _jobEvents = new();
    private readonly ConcurrentDictionary<string, string> _fileContents = new();

    public InMemoryFineTuningRepository(ILogger<InMemoryFineTuningRepository> logger)
    {
        _logger = logger;
    }

    public Task<IEnumerable<FineTuningJob>> GetAllJobsAsync(string userId)
    {
        _logger.LogDebug("Getting all fine-tuning jobs for user {UserId}", userId);

        var jobs = _jobs.Values
            .Where(j => j.CreatedBy == userId)
            .OrderByDescending(j => j.CreatedAt);

        return Task.FromResult(jobs.AsEnumerable());
    }

    public Task<FineTuningJob?> GetJobByIdAsync(string jobId)
    {
        _logger.LogDebug("Getting fine-tuning job {JobId}", jobId);

        _jobs.TryGetValue(jobId, out var job);
        return Task.FromResult(job);
    }

    public Task<FineTuningJob?> GetJobByProviderJobIdAsync(string providerJobId, string provider)
    {
        _logger.LogDebug("Getting fine-tuning job by provider job ID {ProviderJobId} for provider {Provider}", providerJobId, provider);

        var job = _jobs.Values.FirstOrDefault(j => j.ProviderJobId == providerJobId && j.Provider == provider);
        return Task.FromResult(job);
    }

    public Task<FineTuningJob> CreateJobAsync(FineTuningJob job)
    {
        _logger.LogDebug("Creating fine-tuning job {JobId}", job.Id);

        job.Id = job.Id ?? Guid.NewGuid().ToString();
        job.CreatedAt = DateTime.UtcNow;

        _jobs[job.Id] = job;
        _jobEvents[job.Id] = new List<FineTuningStepMetric>();

        return Task.FromResult(job);
    }

    public Task<FineTuningJob> UpdateJobAsync(FineTuningJob job)
    {
        _logger.LogDebug("Updating fine-tuning job {JobId}", job.Id);

        if (!_jobs.ContainsKey(job.Id))
        {
            throw new KeyNotFoundException($"Fine-tuning job {job.Id} not found");
        }

        _jobs[job.Id] = job;

        return Task.FromResult(job);
    }

    public Task DeleteJobAsync(string jobId)
    {
        _logger.LogDebug("Deleting fine-tuning job {JobId}", jobId);

        _jobs.TryRemove(jobId, out _);
        _jobEvents.TryRemove(jobId, out _);

        return Task.CompletedTask;
    }

    public Task<IEnumerable<FineTuningStepMetric>> GetJobEventsAsync(string jobId)
    {
        _logger.LogDebug("Getting events for fine-tuning job {JobId}", jobId);

        if (_jobEvents.TryGetValue(jobId, out var events))
        {
            return Task.FromResult(events.OrderBy(e => e.Step).AsEnumerable());
        }

        return Task.FromResult(Enumerable.Empty<FineTuningStepMetric>());
    }

    public Task AddJobEventAsync(string jobId, FineTuningStepMetric metric)
    {
        _logger.LogDebug("Adding event for fine-tuning job {JobId}, step {Step}", jobId, metric.Step);

        if (!_jobEvents.ContainsKey(jobId))
        {
            _jobEvents[jobId] = new List<FineTuningStepMetric>();
        }

        _jobEvents[jobId].Add(metric);

        return Task.CompletedTask;
    }

    public Task<(IEnumerable<FineTuningJob> Jobs, int TotalCount)> SearchJobsAsync(
        string userId,
        string? query,
        FineTuningJobStatus? status,
        string? provider,
        string? baseModelId,
        IEnumerable<string>? tags,
        string? createdBy,
        int page,
        int pageSize)
    {
        _logger.LogDebug("Searching fine-tuning jobs for user {UserId}", userId);

        var queryable = _jobs.Values
            .Where(j => j.CreatedBy == userId)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(query))
        {
            queryable = queryable.Where(j =>
                j.BaseModelId.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                j.Provider.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        if (status.HasValue)
        {
            queryable = queryable.Where(j => j.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(provider))
        {
            queryable = queryable.Where(j => j.Provider == provider);
        }

        if (!string.IsNullOrWhiteSpace(baseModelId))
        {
            queryable = queryable.Where(j => j.BaseModelId == baseModelId);
        }

        if (tags != null && tags.Any())
        {
            var tagList = tags.ToList();
            queryable = queryable.Where(j => j.Tags.Any(tag => tagList.Contains(tag)));
        }

        if (!string.IsNullOrWhiteSpace(createdBy))
        {
            queryable = queryable.Where(j => j.CreatedBy == createdBy);
        }

        var totalCount = queryable.Count();

        var jobs = queryable
            .OrderByDescending(j => j.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult((jobs.AsEnumerable(), totalCount));
    }

    public Task<IEnumerable<FineTuningFile>> GetAllFilesAsync(string userId)
    {
        _logger.LogDebug("Getting all fine-tuning files for user {UserId}", userId);

        var files = _files.Values
            .Where(f => f.CreatedBy == userId)
            .OrderByDescending(f => f.CreatedAt);

        return Task.FromResult(files.AsEnumerable());
    }

    public Task<FineTuningFile?> GetFileByIdAsync(string fileId)
    {
        _logger.LogDebug("Getting fine-tuning file {FileId}", fileId);

        _files.TryGetValue(fileId, out var file);
        return Task.FromResult(file);
    }

    public Task<FineTuningFile?> GetFileByProviderFileIdAsync(string providerFileId, string provider)
    {
        _logger.LogDebug("Getting fine-tuning file by provider file ID {ProviderFileId} for provider {Provider}", providerFileId, provider);

        var file = _files.Values.FirstOrDefault(f => f.ProviderFileId == providerFileId && f.Provider == provider);
        return Task.FromResult(file);
    }

    public Task<FineTuningFile> CreateFileAsync(FineTuningFile file)
    {
        _logger.LogDebug("Creating fine-tuning file {FileId}", file.Id);

        file.Id = file.Id ?? Guid.NewGuid().ToString();
        file.CreatedAt = DateTime.UtcNow;

        _files[file.Id] = file;

        return Task.FromResult(file);
    }

    public Task<FineTuningFile> UpdateFileAsync(FineTuningFile file)
    {
        _logger.LogDebug("Updating fine-tuning file {FileId}", file.Id);

        if (!_files.ContainsKey(file.Id))
        {
            throw new KeyNotFoundException($"Fine-tuning file {file.Id} not found");
        }

        _files[file.Id] = file;

        return Task.FromResult(file);
    }

    public Task DeleteFileAsync(string fileId)
    {
        _logger.LogDebug("Deleting fine-tuning file {FileId}", fileId);

        _files.TryRemove(fileId, out _);
        _fileContents.TryRemove(fileId, out _);

        return Task.CompletedTask;
    }

    public Task SaveFileContentAsync(string fileId, string content)
    {
        _logger.LogDebug("Saving content for fine-tuning file {FileId}", fileId);

        _fileContents[fileId] = content;

        return Task.CompletedTask;
    }

    public Task<string?> GetFileContentAsync(string fileId)
    {
        _logger.LogDebug("Getting content for fine-tuning file {FileId}", fileId);

        _fileContents.TryGetValue(fileId, out var content);
        return Task.FromResult(content);
    }

    public Task<IEnumerable<FineTuningJob>> GetJobsByStatusAsync(FineTuningJobStatus status)
    {
        _logger.LogDebug("Getting fine-tuning jobs by status {Status}", status);

        var jobs = _jobs.Values
            .Where(j => j.Status == status)
            .OrderByDescending(j => j.CreatedAt);

        return Task.FromResult(jobs.AsEnumerable());
    }
}
