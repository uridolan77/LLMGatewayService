using LLMGateway.Core.Models.FineTuning;

namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Interface for fine-tuning repository
/// </summary>
public interface IFineTuningRepository
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
    /// <returns>Fine-tuning job</returns>
    Task<FineTuningJob?> GetJobByIdAsync(string jobId);
    
    /// <summary>
    /// Get fine-tuning job by provider job ID
    /// </summary>
    /// <param name="providerJobId">Provider job ID</param>
    /// <param name="provider">Provider</param>
    /// <returns>Fine-tuning job</returns>
    Task<FineTuningJob?> GetJobByProviderJobIdAsync(string providerJobId, string provider);
    
    /// <summary>
    /// Create fine-tuning job
    /// </summary>
    /// <param name="job">Fine-tuning job</param>
    /// <returns>Created fine-tuning job</returns>
    Task<FineTuningJob> CreateJobAsync(FineTuningJob job);
    
    /// <summary>
    /// Update fine-tuning job
    /// </summary>
    /// <param name="job">Fine-tuning job</param>
    /// <returns>Updated fine-tuning job</returns>
    Task<FineTuningJob> UpdateJobAsync(FineTuningJob job);
    
    /// <summary>
    /// Delete fine-tuning job
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <returns>Task</returns>
    Task DeleteJobAsync(string jobId);
    
    /// <summary>
    /// Get fine-tuning job events
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <returns>List of fine-tuning step metrics</returns>
    Task<IEnumerable<FineTuningStepMetric>> GetJobEventsAsync(string jobId);
    
    /// <summary>
    /// Add fine-tuning job event
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <param name="metric">Step metric</param>
    /// <returns>Task</returns>
    Task AddJobEventAsync(string jobId, FineTuningStepMetric metric);
    
    /// <summary>
    /// Search fine-tuning jobs
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="query">Search query</param>
    /// <param name="status">Filter by status</param>
    /// <param name="provider">Filter by provider</param>
    /// <param name="baseModelId">Filter by base model ID</param>
    /// <param name="tags">Filter by tags</param>
    /// <param name="createdBy">Filter by created by</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Search results</returns>
    Task<(IEnumerable<FineTuningJob> Jobs, int TotalCount)> SearchJobsAsync(
        string userId,
        string? query,
        FineTuningJobStatus? status,
        string? provider,
        string? baseModelId,
        IEnumerable<string>? tags,
        string? createdBy,
        int page,
        int pageSize);
    
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
    /// <returns>Fine-tuning file</returns>
    Task<FineTuningFile?> GetFileByIdAsync(string fileId);
    
    /// <summary>
    /// Get fine-tuning file by provider file ID
    /// </summary>
    /// <param name="providerFileId">Provider file ID</param>
    /// <param name="provider">Provider</param>
    /// <returns>Fine-tuning file</returns>
    Task<FineTuningFile?> GetFileByProviderFileIdAsync(string providerFileId, string provider);
    
    /// <summary>
    /// Create fine-tuning file
    /// </summary>
    /// <param name="file">Fine-tuning file</param>
    /// <returns>Created fine-tuning file</returns>
    Task<FineTuningFile> CreateFileAsync(FineTuningFile file);
    
    /// <summary>
    /// Update fine-tuning file
    /// </summary>
    /// <param name="file">Fine-tuning file</param>
    /// <returns>Updated fine-tuning file</returns>
    Task<FineTuningFile> UpdateFileAsync(FineTuningFile file);
    
    /// <summary>
    /// Delete fine-tuning file
    /// </summary>
    /// <param name="fileId">File ID</param>
    /// <returns>Task</returns>
    Task DeleteFileAsync(string fileId);
    
    /// <summary>
    /// Save file content
    /// </summary>
    /// <param name="fileId">File ID</param>
    /// <param name="content">File content</param>
    /// <returns>Task</returns>
    Task SaveFileContentAsync(string fileId, string content);
    
    /// <summary>
    /// Get file content
    /// </summary>
    /// <param name="fileId">File ID</param>
    /// <returns>File content</returns>
    Task<string?> GetFileContentAsync(string fileId);
    
    /// <summary>
    /// Get jobs by status
    /// </summary>
    /// <param name="status">Status</param>
    /// <returns>List of fine-tuning jobs</returns>
    Task<IEnumerable<FineTuningJob>> GetJobsByStatusAsync(FineTuningJobStatus status);
}
