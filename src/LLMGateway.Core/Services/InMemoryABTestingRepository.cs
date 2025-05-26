using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Routing;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace LLMGateway.Core.Services;

/// <summary>
/// In-memory implementation of IABTestingRepository for when database is not available
/// </summary>
public class InMemoryABTestingRepository : IABTestingRepository
{
    private readonly ILogger<InMemoryABTestingRepository> _logger;
    private readonly ConcurrentDictionary<string, ABTestingExperiment> _experiments = new();
    private readonly ConcurrentDictionary<string, ABTestingResult> _results = new();
    private readonly ConcurrentDictionary<string, Dictionary<string, string>> _userGroupAssignments = new();

    public InMemoryABTestingRepository(ILogger<InMemoryABTestingRepository> logger)
    {
        _logger = logger;
    }

    public Task<IEnumerable<ABTestingExperiment>> GetAllExperimentsAsync(bool includeInactive = false)
    {
        _logger.LogDebug("Getting all experiments, includeInactive: {IncludeInactive}", includeInactive);
        
        var experiments = _experiments.Values.AsQueryable();
        
        if (!includeInactive)
        {
            experiments = experiments.Where(e => e.IsActive);
        }
        
        return Task.FromResult(experiments.OrderByDescending(e => e.CreatedAt).AsEnumerable());
    }

    public Task<ABTestingExperiment?> GetExperimentByIdAsync(string experimentId)
    {
        _logger.LogDebug("Getting experiment {ExperimentId}", experimentId);
        
        _experiments.TryGetValue(experimentId, out var experiment);
        return Task.FromResult(experiment);
    }

    public Task<ABTestingExperiment> CreateExperimentAsync(ABTestingExperiment experiment)
    {
        _logger.LogDebug("Creating experiment {ExperimentId}", experiment.Id);
        
        experiment.Id = experiment.Id ?? Guid.NewGuid().ToString();
        experiment.CreatedAt = DateTime.UtcNow;
        
        _experiments[experiment.Id] = experiment;
        
        // Initialize user group assignments for this experiment
        _userGroupAssignments[experiment.Id] = new Dictionary<string, string>();
        
        return Task.FromResult(experiment);
    }

    public Task<ABTestingExperiment> UpdateExperimentAsync(ABTestingExperiment experiment)
    {
        _logger.LogDebug("Updating experiment {ExperimentId}", experiment.Id);
        
        if (!_experiments.ContainsKey(experiment.Id))
        {
            throw new KeyNotFoundException($"Experiment {experiment.Id} not found");
        }
        
        _experiments[experiment.Id] = experiment;
        
        return Task.FromResult(experiment);
    }

    public Task DeleteExperimentAsync(string experimentId)
    {
        _logger.LogDebug("Deleting experiment {ExperimentId}", experimentId);
        
        _experiments.TryRemove(experimentId, out _);
        _userGroupAssignments.TryRemove(experimentId, out _);
        
        // Remove associated results
        var resultsToRemove = _results.Values
            .Where(r => r.ExperimentId == experimentId)
            .Select(r => r.Id)
            .ToList();
            
        foreach (var resultId in resultsToRemove)
        {
            _results.TryRemove(resultId, out _);
        }
        
        return Task.CompletedTask;
    }

    public Task<IEnumerable<ABTestingResult>> GetExperimentResultsAsync(string experimentId)
    {
        _logger.LogDebug("Getting results for experiment {ExperimentId}", experimentId);
        
        var results = _results.Values
            .Where(r => r.ExperimentId == experimentId)
            .OrderByDescending(r => r.Timestamp);
            
        return Task.FromResult(results.AsEnumerable());
    }

    public Task<ABTestingResult> CreateResultAsync(ABTestingResult result)
    {
        _logger.LogDebug("Creating result for experiment {ExperimentId}", result.ExperimentId);
        
        result.Id = result.Id ?? Guid.NewGuid().ToString();
        result.Timestamp = DateTime.UtcNow;
        
        _results[result.Id] = result;
        
        return Task.FromResult(result);
    }

    public Task<IEnumerable<ABTestingExperiment>> GetActiveExperimentsForModelAsync(string modelId)
    {
        _logger.LogDebug("Getting active experiments for model {ModelId}", modelId);
        
        var experiments = _experiments.Values
            .Where(e => e.IsActive && 
                       (e.ControlModelId == modelId || e.TreatmentModelId == modelId))
            .OrderByDescending(e => e.CreatedAt);
            
        return Task.FromResult(experiments.AsEnumerable());
    }

    public Task<string?> GetUserGroupAssignmentAsync(string experimentId, string userId)
    {
        _logger.LogDebug("Getting user group assignment for experiment {ExperimentId}, user {UserId}", experimentId, userId);
        
        if (_userGroupAssignments.TryGetValue(experimentId, out var assignments) &&
            assignments.TryGetValue(userId, out var group))
        {
            return Task.FromResult<string?>(group);
        }
        
        return Task.FromResult<string?>(null);
    }

    public Task SetUserGroupAssignmentAsync(string experimentId, string userId, string group)
    {
        _logger.LogDebug("Setting user group assignment for experiment {ExperimentId}, user {UserId}, group {Group}", 
            experimentId, userId, group);
        
        if (!_userGroupAssignments.ContainsKey(experimentId))
        {
            _userGroupAssignments[experimentId] = new Dictionary<string, string>();
        }
        
        _userGroupAssignments[experimentId][userId] = group;
        
        return Task.CompletedTask;
    }
}
