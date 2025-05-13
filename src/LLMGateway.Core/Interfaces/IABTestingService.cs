using LLMGateway.Core.Models.Routing;

namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Interface for A/B testing service
/// </summary>
public interface IABTestingService
{
    /// <summary>
    /// Get all experiments
    /// </summary>
    /// <param name="includeInactive">Whether to include inactive experiments</param>
    /// <returns>List of experiments</returns>
    Task<IEnumerable<ABTestingExperiment>> GetAllExperimentsAsync(bool includeInactive = false);
    
    /// <summary>
    /// Get experiment by ID
    /// </summary>
    /// <param name="experimentId">Experiment ID</param>
    /// <returns>Experiment</returns>
    Task<ABTestingExperiment> GetExperimentAsync(string experimentId);
    
    /// <summary>
    /// Create experiment
    /// </summary>
    /// <param name="request">Create request</param>
    /// <param name="userId">User ID</param>
    /// <returns>Created experiment</returns>
    Task<ABTestingExperiment> CreateExperimentAsync(ABTestingExperimentCreateRequest request, string userId);
    
    /// <summary>
    /// Update experiment
    /// </summary>
    /// <param name="experimentId">Experiment ID</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated experiment</returns>
    Task<ABTestingExperiment> UpdateExperimentAsync(string experimentId, ABTestingExperimentUpdateRequest request);
    
    /// <summary>
    /// Delete experiment
    /// </summary>
    /// <param name="experimentId">Experiment ID</param>
    /// <returns>Task</returns>
    Task DeleteExperimentAsync(string experimentId);
    
    /// <summary>
    /// Get experiment results
    /// </summary>
    /// <param name="experimentId">Experiment ID</param>
    /// <returns>List of results</returns>
    Task<IEnumerable<ABTestingResult>> GetExperimentResultsAsync(string experimentId);
    
    /// <summary>
    /// Create experiment result
    /// </summary>
    /// <param name="request">Create request</param>
    /// <param name="userId">User ID</param>
    /// <returns>Created result</returns>
    Task<ABTestingResult> CreateResultAsync(ABTestingResultCreateRequest request, string userId);
    
    /// <summary>
    /// Get experiment statistics
    /// </summary>
    /// <param name="experimentId">Experiment ID</param>
    /// <returns>Experiment statistics</returns>
    Task<ABTestingExperimentStatistics> GetExperimentStatisticsAsync(string experimentId);
    
    /// <summary>
    /// Assign user to experiment group
    /// </summary>
    /// <param name="experimentId">Experiment ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Group assignment (control or treatment)</returns>
    Task<string> AssignUserToGroupAsync(string experimentId, string userId);
    
    /// <summary>
    /// Get model for user
    /// </summary>
    /// <param name="requestedModelId">Requested model ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Model ID to use</returns>
    Task<string> GetModelForUserAsync(string requestedModelId, string userId);
    
    /// <summary>
    /// Get active experiments for model
    /// </summary>
    /// <param name="modelId">Model ID</param>
    /// <returns>List of experiments</returns>
    Task<IEnumerable<ABTestingExperiment>> GetActiveExperimentsForModelAsync(string modelId);
}
