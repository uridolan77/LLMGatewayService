using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.FineTuning;
using LLMGateway.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LLMGateway.Infrastructure.Repositories;

/// <summary>
/// Repository for fine-tuning operations
/// </summary>
public class FineTuningRepository : IFineTuningRepository
{
    private readonly LLMGatewayDbContext _dbContext;
    private readonly ILogger<FineTuningRepository> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="dbContext">Database context</param>
    /// <param name="logger">Logger</param>
    public FineTuningRepository(
        LLMGatewayDbContext dbContext,
        ILogger<FineTuningRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<FineTuningJob>> GetAllJobsAsync(string userId)
    {
        try
        {
            var entities = await _dbContext.FineTuningJobs
                .Where(j => j.CreatedBy == userId)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();
                
            return entities.Select(e => e.ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all fine-tuning jobs for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<FineTuningJob?> GetJobByIdAsync(string jobId)
    {
        try
        {
            var entity = await _dbContext.FineTuningJobs
                .FirstOrDefaultAsync(j => j.Id == jobId);
                
            return entity?.ToDomainModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get fine-tuning job {JobId}", jobId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<FineTuningJob?> GetJobByProviderJobIdAsync(string providerJobId, string provider)
    {
        try
        {
            var entity = await _dbContext.FineTuningJobs
                .FirstOrDefaultAsync(j => j.ProviderJobId == providerJobId && j.Provider == provider);
                
            return entity?.ToDomainModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get fine-tuning job by provider job ID {ProviderJobId}", providerJobId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<FineTuningJob> CreateJobAsync(FineTuningJob job)
    {
        try
        {
            var entity = Persistence.Entities.FineTuningJob.FromDomainModel(job);
            
            _dbContext.FineTuningJobs.Add(entity);
            await _dbContext.SaveChangesAsync();
            
            return entity.ToDomainModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create fine-tuning job {JobId}", job.Id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<FineTuningJob> UpdateJobAsync(FineTuningJob job)
    {
        try
        {
            var entity = await _dbContext.FineTuningJobs
                .FirstOrDefaultAsync(j => j.Id == job.Id);
                
            if (entity == null)
            {
                throw new Exception($"Fine-tuning job with ID {job.Id} not found");
            }
            
            // Update the entity
            entity.Name = job.Name;
            entity.Description = job.Description;
            entity.FineTunedModelId = job.FineTunedModelId;
            entity.ValidationFileId = job.ValidationFileId;
            entity.Status = job.Status;
            entity.StartedAt = job.StartedAt;
            entity.CompletedAt = job.CompletedAt;
            entity.ErrorMessage = job.ErrorMessage;
            entity.ProviderJobId = job.ProviderJobId;
            
            entity.SetHyperparameters(job.Hyperparameters);
            entity.SetMetrics(job.Metrics);
            entity.SetTags(job.Tags);
            
            _dbContext.FineTuningJobs.Update(entity);
            await _dbContext.SaveChangesAsync();
            
            return entity.ToDomainModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update fine-tuning job {JobId}", job.Id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteJobAsync(string jobId)
    {
        try
        {
            var entity = await _dbContext.FineTuningJobs
                .FirstOrDefaultAsync(j => j.Id == jobId);
                
            if (entity != null)
            {
                _dbContext.FineTuningJobs.Remove(entity);
                await _dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete fine-tuning job {JobId}", jobId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<FineTuningStepMetric>> GetJobEventsAsync(string jobId)
    {
        try
        {
            var entities = await _dbContext.FineTuningStepMetrics
                .Where(e => e.JobId == jobId)
                .OrderBy(e => e.Step)
                .ToListAsync();
                
            return entities.Select(e => e.ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get events for fine-tuning job {JobId}", jobId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task AddJobEventAsync(string jobId, FineTuningStepMetric metric)
    {
        try
        {
            // Check if the event already exists
            var existingEntity = await _dbContext.FineTuningStepMetrics
                .FirstOrDefaultAsync(e => e.JobId == jobId && e.Step == metric.Step);
                
            if (existingEntity != null)
            {
                // Update the existing entity
                existingEntity.Timestamp = metric.Timestamp;
                existingEntity.Loss = metric.Loss;
                existingEntity.Accuracy = metric.Accuracy;
                existingEntity.ElapsedTokens = metric.ElapsedTokens;
                
                _dbContext.FineTuningStepMetrics.Update(existingEntity);
            }
            else
            {
                // Create a new entity
                var entity = Persistence.Entities.FineTuningStepMetric.FromDomainModel(jobId, metric);
                _dbContext.FineTuningStepMetrics.Add(entity);
            }
            
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add event for fine-tuning job {JobId}", jobId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<(IEnumerable<FineTuningJob> Jobs, int TotalCount)> SearchJobsAsync(
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
        try
        {
            var queryable = _dbContext.FineTuningJobs.AsQueryable();
            
            // Apply filters
            queryable = queryable.Where(j => j.CreatedBy == userId);
            
            if (!string.IsNullOrWhiteSpace(query))
            {
                queryable = queryable.Where(j =>
                    j.Name.Contains(query) ||
                    j.Description.Contains(query) ||
                    j.BaseModelId.Contains(query) ||
                    j.FineTunedModelId != null && j.FineTunedModelId.Contains(query));
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
                
                // This is a simple approach - in a real implementation, you would use a more sophisticated
                // approach to search for tags in the JSON array
                queryable = queryable.Where(j => tagList.Any(tag => j.TagsJson.Contains(tag)));
            }
            
            if (!string.IsNullOrWhiteSpace(createdBy))
            {
                queryable = queryable.Where(j => j.CreatedBy == createdBy);
            }
            
            // Get total count
            var totalCount = await queryable.CountAsync();
            
            // Apply pagination
            var entities = await queryable
                .OrderByDescending(j => j.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
                
            return (entities.Select(e => e.ToDomainModel()).ToList(), totalCount);
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
            var entities = await _dbContext.FineTuningFiles
                .Where(f => f.CreatedBy == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
                
            return entities.Select(e => e.ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all fine-tuning files for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<FineTuningFile?> GetFileByIdAsync(string fileId)
    {
        try
        {
            var entity = await _dbContext.FineTuningFiles
                .FirstOrDefaultAsync(f => f.Id == fileId);
                
            return entity?.ToDomainModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get fine-tuning file {FileId}", fileId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<FineTuningFile?> GetFileByProviderFileIdAsync(string providerFileId, string provider)
    {
        try
        {
            var entity = await _dbContext.FineTuningFiles
                .FirstOrDefaultAsync(f => f.ProviderFileId == providerFileId && f.Provider == provider);
                
            return entity?.ToDomainModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get fine-tuning file by provider file ID {ProviderFileId}", providerFileId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<FineTuningFile> CreateFileAsync(FineTuningFile file)
    {
        try
        {
            var entity = Persistence.Entities.FineTuningFile.FromDomainModel(file);
            
            _dbContext.FineTuningFiles.Add(entity);
            await _dbContext.SaveChangesAsync();
            
            return entity.ToDomainModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create fine-tuning file {FileId}", file.Id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<FineTuningFile> UpdateFileAsync(FineTuningFile file)
    {
        try
        {
            var entity = await _dbContext.FineTuningFiles
                .FirstOrDefaultAsync(f => f.Id == file.Id);
                
            if (entity == null)
            {
                throw new Exception($"Fine-tuning file with ID {file.Id} not found");
            }
            
            // Update the entity
            entity.Name = file.Name;
            entity.Size = file.Size;
            entity.Purpose = file.Purpose;
            entity.ProviderFileId = file.ProviderFileId;
            entity.Status = file.Status;
            
            _dbContext.FineTuningFiles.Update(entity);
            await _dbContext.SaveChangesAsync();
            
            return entity.ToDomainModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update fine-tuning file {FileId}", file.Id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteFileAsync(string fileId)
    {
        try
        {
            var entity = await _dbContext.FineTuningFiles
                .FirstOrDefaultAsync(f => f.Id == fileId);
                
            if (entity != null)
            {
                _dbContext.FineTuningFiles.Remove(entity);
                await _dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete fine-tuning file {FileId}", fileId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task SaveFileContentAsync(string fileId, string content)
    {
        try
        {
            var entity = await _dbContext.FineTuningFileContents
                .FirstOrDefaultAsync(c => c.FileId == fileId);
                
            if (entity != null)
            {
                // Update the existing entity
                entity.Content = content;
                _dbContext.FineTuningFileContents.Update(entity);
            }
            else
            {
                // Create a new entity
                entity = new Persistence.Entities.FineTuningFileContent
                {
                    FileId = fileId,
                    Content = content
                };
                
                _dbContext.FineTuningFileContents.Add(entity);
            }
            
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save content for fine-tuning file {FileId}", fileId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string?> GetFileContentAsync(string fileId)
    {
        try
        {
            var entity = await _dbContext.FineTuningFileContents
                .FirstOrDefaultAsync(c => c.FileId == fileId);
                
            return entity?.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get content for fine-tuning file {FileId}", fileId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<FineTuningJob>> GetJobsByStatusAsync(FineTuningJobStatus status)
    {
        try
        {
            var entities = await _dbContext.FineTuningJobs
                .Where(j => j.Status == status)
                .ToListAsync();
                
            return entities.Select(e => e.ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get fine-tuning jobs by status {Status}", status);
            throw;
        }
    }
}
