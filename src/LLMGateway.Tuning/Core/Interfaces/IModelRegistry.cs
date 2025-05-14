using System.Collections.Generic;
using System.Threading.Tasks;
using LLMGateway.Tuning.Deployment.Registry;

namespace LLMGateway.Tuning.Core.Interfaces
{
    public interface IModelRegistry
    {
        Task<string> RegisterModelAsync(ModelVersion model);
        Task<ModelVersion> GetModelAsync(string id);
        Task<List<ModelVersion>> ListModelsAsync(int limit = 100, int offset = 0);
        Task<bool> UpdateModelStatusAsync(string id, ModelStatus status);
        Task<bool> DeleteModelAsync(string id);
    }
}
