using System;
using System.Threading.Tasks;
using LLMGateway.Tuning.Core.Models;
using LLMGateway.Tuning.Training.Adapters;
using LLMGateway.Tuning.Training.Configuration;

namespace LLMGateway.Tuning.Core.Interfaces
{
    public interface IModelAdapter
    {
        Task<TrainingResult> TrainModelAsync(
            string trainingData,
            string validationData,
            TrainingConfiguration config);
            
        Task<string> PredictAsync(string modelId, string prompt);
        
        Task<string> CreateFineTuningJobAsync(string fileId, TrainingConfiguration config);
        
        Task<string> UploadDatasetAsync(string formattedData);
        
        Task<TrainingJobStatus> GetJobStatusAsync(string jobId);
    }
}
