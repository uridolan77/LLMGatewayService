using LLMGateway.Core.Models.FineTuning;

namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Interface for fine-tuning provider
/// </summary>
public interface IFineTuningProvider
{
    /// <summary>
    /// Provider name
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Check if provider supports fine-tuning
    /// </summary>
    /// <returns>True if provider supports fine-tuning</returns>
    bool SupportsFineTuning { get; }
    
    /// <summary>
    /// Get supported base models for fine-tuning
    /// </summary>
    /// <returns>List of model IDs</returns>
    Task<IEnumerable<string>> GetSupportedBaseModelsAsync();
    
    /// <summary>
    /// Create fine-tuning job
    /// </summary>
    /// <param name="job">Fine-tuning job</param>
    /// <returns>Provider job ID</returns>
    Task<string> CreateFineTuningJobAsync(FineTuningJob job);
    
    /// <summary>
    /// Get fine-tuning job
    /// </summary>
    /// <param name="providerJobId">Provider job ID</param>
    /// <returns>Fine-tuning job details</returns>
    Task<(FineTuningJobStatus Status, string? FineTunedModelId, string? ErrorMessage, FineTuningMetrics? Metrics)> GetFineTuningJobAsync(string providerJobId);
    
    /// <summary>
    /// Cancel fine-tuning job
    /// </summary>
    /// <param name="providerJobId">Provider job ID</param>
    /// <returns>Task</returns>
    Task CancelFineTuningJobAsync(string providerJobId);
    
    /// <summary>
    /// Delete fine-tuned model
    /// </summary>
    /// <param name="fineTunedModelId">Fine-tuned model ID</param>
    /// <returns>Task</returns>
    Task DeleteFineTunedModelAsync(string fineTunedModelId);
    
    /// <summary>
    /// Get fine-tuning job events
    /// </summary>
    /// <param name="providerJobId">Provider job ID</param>
    /// <returns>List of fine-tuning step metrics</returns>
    Task<IEnumerable<FineTuningStepMetric>> GetFineTuningJobEventsAsync(string providerJobId);
    
    /// <summary>
    /// Upload fine-tuning file
    /// </summary>
    /// <param name="fileName">File name</param>
    /// <param name="purpose">File purpose</param>
    /// <param name="content">File content</param>
    /// <returns>Provider file ID</returns>
    Task<string> UploadFineTuningFileAsync(string fileName, string purpose, string content);
    
    /// <summary>
    /// Get fine-tuning file
    /// </summary>
    /// <param name="providerFileId">Provider file ID</param>
    /// <returns>File details</returns>
    Task<(string Name, long Size, string Status)> GetFineTuningFileAsync(string providerFileId);
    
    /// <summary>
    /// Delete fine-tuning file
    /// </summary>
    /// <param name="providerFileId">Provider file ID</param>
    /// <returns>Task</returns>
    Task DeleteFineTuningFileAsync(string providerFileId);
    
    /// <summary>
    /// Get fine-tuning file content
    /// </summary>
    /// <param name="providerFileId">Provider file ID</param>
    /// <returns>File content</returns>
    Task<string> GetFineTuningFileContentAsync(string providerFileId);
}
