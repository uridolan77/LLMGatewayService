using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LLMGateway.Tuning.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace LLMGateway.Tuning.Deployment.Registry
{
    public class ModelRegistry : IModelRegistry
    {
        private readonly ILogger<ModelRegistry> _logger;
        // In a real implementation, this would use a database or other persistent storage
        private readonly Dictionary<string, ModelVersion> _models = new Dictionary<string, ModelVersion>();

        public ModelRegistry(ILogger<ModelRegistry> logger)
        {
            _logger = logger;
        }

        public async Task<string> RegisterModelAsync(ModelVersion model)
        {            try
            {
                var modelId = string.IsNullOrEmpty(model.Id) ? Guid.NewGuid().ToString() : model.Id;
                
                var newModel = model with { Id = modelId };
                _models[modelId] = newModel;
                _logger.LogInformation("Registered model with ID: {ModelId}", modelId);
                
                return modelId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering model");
                throw;
            }
        }

        public async Task<ModelVersion> GetModelAsync(string id)
        {
            try
            {
                if (_models.TryGetValue(id, out var model))
                {
                    return model;
                }
                
                _logger.LogWarning("Model with ID: {ModelId} not found", id);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting model with ID: {ModelId}", id);
                throw;
            }
        }

        public async Task<List<ModelVersion>> ListModelsAsync(int limit = 100, int offset = 0)
        {
            try
            {
                var models = new List<ModelVersion>(_models.Values);
                var result = models.Skip(offset).Take(limit).ToList();
                _logger.LogInformation("Listed {Count} models", result.Count);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing models");
                throw;
            }
        }

        public async Task<bool> UpdateModelStatusAsync(string id, ModelStatus status)
        {
            try
            {
                if (!_models.TryGetValue(id, out var model))
                {
                    _logger.LogWarning("Model with ID: {ModelId} not found for status update", id);
                    return false;
                }
                
                var updatedModel = model with { Status = status };
                _models[id] = updatedModel;
                
                _logger.LogInformation("Updated model {ModelId} status to {Status}", id, status);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating model status");
                return false;
            }
        }

        public async Task<bool> DeleteModelAsync(string id)
        {
            try
            {
                if (!_models.ContainsKey(id))
                {
                    _logger.LogWarning("Model with ID: {ModelId} not found for deletion", id);
                    return false;
                }
                
                _models.Remove(id);
                _logger.LogInformation("Deleted model with ID: {ModelId}", id);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting model");
                return false;
            }
        }
    }

    public record ModelVersion
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public string Name { get; init; }
        public string BaseModelId { get; init; }
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
        public string CreatedBy { get; init; }
        public ModelStatus Status { get; init; } = ModelStatus.Created;
        public Dictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
    }

    public enum ModelStatus
    {
        Created,
        Training,
        TrainingComplete,
        Evaluating,
        EvaluationComplete,
        DeploymentInProgress,
        Deployed,
        Archived,
        DeploymentFailed,
        TrainingFailed,
        Failed
    }
}
