using LLMGateway.Core.Models.Provider;

namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Service for retrieving model information
/// </summary>
public interface IModelService
{
    /// <summary>
    /// Get information about all models
    /// </summary>
    /// <returns>List of model information</returns>
    Task<IEnumerable<ModelInfo>> GetModelsAsync();
    
    /// <summary>
    /// Get information about a specific model
    /// </summary>
    /// <param name="modelId">ID of the model</param>
    /// <returns>Model information</returns>
    Task<ModelInfo> GetModelAsync(string modelId);
}
