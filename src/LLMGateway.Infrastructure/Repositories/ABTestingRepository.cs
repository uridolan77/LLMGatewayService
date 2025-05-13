using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Routing;
using LLMGateway.Infrastructure.Persistence;
using LLMGateway.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LLMGateway.Infrastructure.Repositories;

/// <summary>
/// Repository for A/B testing
/// </summary>
public class ABTestingRepository : IABTestingRepository
{
    private readonly LLMGatewayDbContext _dbContext;
    private readonly ILogger<ABTestingRepository> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="dbContext">Database context</param>
    /// <param name="logger">Logger</param>
    public ABTestingRepository(
        LLMGatewayDbContext dbContext,
        ILogger<ABTestingRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Core.Models.Routing.ABTestingExperiment>> GetAllExperimentsAsync(bool includeInactive = false)
    {
        try
        {
            var query = _dbContext.ABTestingExperiments.AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(e => e.IsActive);
            }

            var entities = await query
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            return entities.Select(e => e.ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all experiments");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Core.Models.Routing.ABTestingExperiment?> GetExperimentByIdAsync(string experimentId)
    {
        try
        {
            var entity = await _dbContext.ABTestingExperiments
                .FirstOrDefaultAsync(e => e.Id == experimentId);

            return entity?.ToDomainModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get experiment {ExperimentId}", experimentId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Core.Models.Routing.ABTestingExperiment> CreateExperimentAsync(Core.Models.Routing.ABTestingExperiment experiment)
    {
        try
        {
            var entity = new Persistence.Entities.ABTestingExperiment
            {
                Id = experiment.Id,
                Name = experiment.Name,
                Description = experiment.Description,
                IsActive = experiment.IsActive,
                StartDate = experiment.StartDate,
                EndDate = experiment.EndDate,
                TrafficAllocationPercentage = experiment.TrafficAllocationPercentage,
                ControlModelId = experiment.ControlModelId,
                TreatmentModelId = experiment.TreatmentModelId,
                UserSegmentsJson = JsonSerializer.Serialize(experiment.UserSegments),
                MetricsJson = JsonSerializer.Serialize(experiment.Metrics),
                CreatedBy = experiment.CreatedBy,
                CreatedAt = experiment.CreatedAt,
                UpdatedAt = experiment.UpdatedAt
            };

            _dbContext.ABTestingExperiments.Add(entity);
            await _dbContext.SaveChangesAsync();

            return entity.ToDomainModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create experiment {ExperimentId}", experiment.Id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Core.Models.Routing.ABTestingExperiment> UpdateExperimentAsync(Core.Models.Routing.ABTestingExperiment experiment)
    {
        try
        {
            var entity = await _dbContext.ABTestingExperiments
                .FirstOrDefaultAsync(e => e.Id == experiment.Id);

            if (entity == null)
            {
                throw new Exception($"Experiment with ID {experiment.Id} not found");
            }

            entity.Name = experiment.Name;
            entity.Description = experiment.Description;
            entity.IsActive = experiment.IsActive;
            entity.EndDate = experiment.EndDate;
            entity.TrafficAllocationPercentage = experiment.TrafficAllocationPercentage;
            entity.ControlModelId = experiment.ControlModelId;
            entity.TreatmentModelId = experiment.TreatmentModelId;
            entity.UserSegmentsJson = JsonSerializer.Serialize(experiment.UserSegments);
            entity.MetricsJson = JsonSerializer.Serialize(experiment.Metrics);
            entity.UpdatedAt = experiment.UpdatedAt;

            _dbContext.ABTestingExperiments.Update(entity);
            await _dbContext.SaveChangesAsync();

            return entity.ToDomainModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update experiment {ExperimentId}", experiment.Id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteExperimentAsync(string experimentId)
    {
        try
        {
            var entity = await _dbContext.ABTestingExperiments
                .FirstOrDefaultAsync(e => e.Id == experimentId);

            if (entity != null)
            {
                _dbContext.ABTestingExperiments.Remove(entity);

                // Also delete all results and user assignments
                var results = await _dbContext.ABTestingResults
                    .Where(r => r.ExperimentId == experimentId)
                    .ToListAsync();

                _dbContext.ABTestingResults.RemoveRange(results);

                var assignments = await _dbContext.ABTestingUserAssignments
                    .Where(a => a.ExperimentId == experimentId)
                    .ToListAsync();

                _dbContext.ABTestingUserAssignments.RemoveRange(assignments);

                await _dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete experiment {ExperimentId}", experimentId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Core.Models.Routing.ABTestingResult>> GetExperimentResultsAsync(string experimentId)
    {
        try
        {
            var entities = await _dbContext.ABTestingResults
                .Where(r => r.ExperimentId == experimentId)
                .OrderByDescending(r => r.Timestamp)
                .ToListAsync();

            return entities.Select(e => e.ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get results for experiment {ExperimentId}", experimentId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Core.Models.Routing.ABTestingResult> CreateResultAsync(Core.Models.Routing.ABTestingResult result)
    {
        try
        {
            var entity = new Persistence.Entities.ABTestingResult
            {
                Id = result.Id,
                ExperimentId = result.ExperimentId,
                UserId = result.UserId,
                RequestId = result.RequestId,
                Group = result.Group,
                ModelId = result.ModelId,
                Timestamp = result.Timestamp,
                MetricsJson = JsonSerializer.Serialize(result.Metrics)
            };

            _dbContext.ABTestingResults.Add(entity);
            await _dbContext.SaveChangesAsync();

            return entity.ToDomainModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create result {ResultId} for experiment {ExperimentId}",
                result.Id, result.ExperimentId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Core.Models.Routing.ABTestingExperiment>> GetActiveExperimentsForModelAsync(string modelId)
    {
        try
        {
            var now = DateTime.UtcNow;

            var entities = await _dbContext.ABTestingExperiments
                .Where(e => e.IsActive &&
                           (e.EndDate == null || e.EndDate > now) &&
                           (e.ControlModelId == modelId || e.TreatmentModelId == modelId))
                .ToListAsync();

            return entities.Select(e => e.ToDomainModel()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active experiments for model {ModelId}", modelId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string?> GetUserGroupAssignmentAsync(string experimentId, string userId)
    {
        try
        {
            var entity = await _dbContext.ABTestingUserAssignments
                .FirstOrDefaultAsync(a => a.ExperimentId == experimentId && a.UserId == userId);

            return entity?.Group;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get group assignment for user {UserId} in experiment {ExperimentId}",
                userId, experimentId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task SetUserGroupAssignmentAsync(string experimentId, string userId, string group)
    {
        try
        {
            var entity = await _dbContext.ABTestingUserAssignments
                .FirstOrDefaultAsync(a => a.ExperimentId == experimentId && a.UserId == userId);

            if (entity == null)
            {
                entity = new Persistence.Entities.ABTestingUserAssignment
                {
                    Id = Guid.NewGuid().ToString(),
                    ExperimentId = experimentId,
                    UserId = userId,
                    Group = group,
                    AssignedAt = DateTime.UtcNow
                };

                _dbContext.ABTestingUserAssignments.Add(entity);
            }
            else
            {
                entity.Group = group;
                entity.AssignedAt = DateTime.UtcNow;

                _dbContext.ABTestingUserAssignments.Update(entity);
            }

            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set group assignment for user {UserId} in experiment {ExperimentId}",
                userId, experimentId);
            throw;
        }
    }
}
