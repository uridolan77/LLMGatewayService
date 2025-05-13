using LLMGateway.Core.Models.FineTuning;

namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Interface for fine-tuning service
/// </summary>
public interface IFineTuningService
{
    /// <summary>
    /// Get all fine-tuning jobs
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of fine-tuning jobs</returns>
    Task<IEnumerable<FineTuningJob>> GetAllJobsAsync(string userId);
    
    /// <summary>
    /// Get fine-tuning job by ID
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Fine-tuning job</returns>
    Task<FineTuningJob> GetJobAsync(string jobId, string userId);
    
    /// <summary>
    /// Create fine-tuning job
    /// </summary>
    /// <param name="request">Create request</param>
    /// <param name="userId">User ID</param>
    /// <returns>Created fine-tuning job</returns>
    Task<FineTuningJob> CreateJobAsync(CreateFineTuningJobRequest request, string userId);
    
    /// <summary>
    /// Cancel fine-tuning job
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Cancelled fine-tuning job</returns>
    Task<FineTuningJob> CancelJobAsync(string jobId, string userId);
    
    /// <summary>
    /// Delete fine-tuning job
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Task</returns>
    Task DeleteJobAsync(string jobId, string userId);
    
    /// <summary>
    /// Get fine-tuning job events
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>List of fine-tuning step metrics</returns>
    Task<IEnumerable<FineTuningStepMetric>> GetJobEventsAsync(string jobId, string userId);
    
    /// <summary>
    /// Search fine-tuning jobs
    /// </summary>
    /// <param name="request">Search request</param>
    /// <param name="userId">User ID</param>
    /// <returns>Search response</returns>
    Task<FineTuningJobSearchResponse> SearchJobsAsync(FineTuningJobSearchRequest request, string userId);
    
    /// <summary>
    /// Get all fine-tuning files
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of fine-tuning files</returns>
    Task<IEnumerable<FineTuningFile>> GetAllFilesAsync(string userId);
    
    /// <summary>
    /// Get fine-tuning file by ID
    /// </summary>
    /// <param name="fileId">File ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Fine-tuning file</returns>
    Task<FineTuningFile> GetFileAsync(string fileId, string userId);
    
    /// <summary>
    /// Upload fine-tuning file
    /// </summary>
    /// <param name="request">Upload request</param>
    /// <param name="userId">User ID</param>
    /// <returns>Uploaded fine-tuning file</returns>
    Task<FineTuningFile> UploadFileAsync(UploadFineTuningFileRequest request, string userId);
    
    /// <summary>
    /// Delete fine-tuning file
    /// </summary>
    /// <param name="fileId">File ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Task</returns>
    Task DeleteFileAsync(string fileId, string userId);
    
    /// <summary>
    /// Get file content
    /// </summary>
    /// <param name="fileId">File ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>File content</returns>
    Task<string> GetFileContentAsync(string fileId, string userId);
    
    /// <summary>
    /// Sync job status with provider
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Updated fine-tuning job</returns>
    Task<FineTuningJob> SyncJobStatusAsync(string jobId, string userId);
    
    /// <summary>
    /// Sync all jobs status with provider
    /// </summary>
    /// <returns>Task</returns>
    Task SyncAllJobsStatusAsync();
}
