using System.Collections.Generic;
using System.Threading.Tasks;
using LLMGateway.Tuning.Core.Models;

namespace LLMGateway.Tuning.Core.Interfaces
{
    public interface IDatasetStorage
    {
        Task<string> SaveDatasetAsync(string datasetId, string splitName, string formattedData);
        Task<string> LoadDatasetAsync(string datasetId, string splitName);
        Task<bool> DeleteDatasetAsync(string datasetId);
        Task<List<Dataset>> ListDatasetsAsync(int limit = 100, int offset = 0);
        Task<Dataset> GetDatasetAsync(string datasetId);
        Task<long> GetDatasetSizeAsync(string datasetId, string splitName);
    }
}
