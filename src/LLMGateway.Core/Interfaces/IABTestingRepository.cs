using LLMGateway.Core.Models.Routing;

namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Interface for A/B testing repository
/// </summary>
public interface IABTestingRepository
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
    Task<ABTestingExperiment?> GetExperimentByIdAsync(string experimentId);
    
    /// <summary>
    /// Create experiment
    /// </summary>
    /// <param name="experiment">Experiment to create</param>
    /// <returns>Created experiment</returns>
    Task<ABTestingExperiment> CreateExperimentAsync(ABTestingExperiment experiment);
    
    /// <summary>
    /// Update experiment
    /// </summary>
    /// <param name="experiment">Experiment to update</param>
    /// <returns>Updated experiment</returns>
    Task<ABTestingExperiment> UpdateExperimentAsync(ABTestingExperiment experiment);
    
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
    /// <param name="result">Result to create</param>
    /// <returns>Created result</returns>
    Task<ABTestingResult> CreateResultAsync(ABTestingResult result);
    
    /// <summary>
    /// Get active experiments for model
    /// </summary>
    /// <param name="modelId">Model ID</param>
    /// <returns>List of experiments</returns>
    Task<IEnumerable<ABTestingExperiment>> GetActiveExperimentsForModelAsync(string modelId);
    
    /// <summary>
    /// Get user group assignment
    /// </summary>
    /// <param name="experimentId">Experiment ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Group assignment (control or treatment)</returns>
    Task<string?> GetUserGroupAssignmentAsync(string experimentId, string userId);
    
    /// <summary>
    /// Set user group assignment
    /// </summary>
    /// <param name="experimentId">Experiment ID</param>
    /// <param name="userId">User ID</param>
    /// <param name="group">Group assignment (control or treatment)</param>
    /// <returns>Task</returns>
    Task SetUserGroupAssignmentAsync(string experimentId, string userId, string group);
}
