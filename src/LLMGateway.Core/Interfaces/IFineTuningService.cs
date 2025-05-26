using LLMGateway.Core.Models.FineTuning;
using LLMGateway.Core.Models.Analytics;

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

    // Phase 3 Advanced Features

    /// <summary>
    /// Get comprehensive fine-tuning analytics
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="startDate">Start date for analytics</param>
    /// <param name="endDate">End date for analytics</param>
    /// <returns>Fine-tuning analytics</returns>
    Task<FineTuningAnalytics> GetAnalyticsAsync(string userId, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Get fine-tuning cost breakdown
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <returns>Cost breakdown</returns>
    Task<FineTuningCostBreakdown> GetCostBreakdownAsync(string userId, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Estimate fine-tuning cost before starting
    /// </summary>
    /// <param name="request">Cost estimation request</param>
    /// <param name="userId">User ID</param>
    /// <returns>Cost estimation</returns>
    Task<FineTuningCostEstimate> EstimateCostAsync(EstimateFineTuningCostRequest request, string userId);

    /// <summary>
    /// Get model performance comparison
    /// </summary>
    /// <param name="baseModelId">Base model ID</param>
    /// <param name="fineTunedModelId">Fine-tuned model ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Performance comparison</returns>
    Task<ModelPerformanceComparison> CompareModelPerformanceAsync(string baseModelId, string fineTunedModelId, string userId);

    /// <summary>
    /// Get fine-tuning recommendations
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="useCase">Use case description</param>
    /// <returns>Fine-tuning recommendations</returns>
    Task<FineTuningRecommendations> GetRecommendationsAsync(string userId, string useCase);

    /// <summary>
    /// Validate training data quality
    /// </summary>
    /// <param name="fileId">File ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Data quality report</returns>
    Task<DataQualityReport> ValidateDataQualityAsync(string fileId, string userId);

    /// <summary>
    /// Get fine-tuning templates
    /// </summary>
    /// <param name="provider">Provider name</param>
    /// <param name="useCase">Use case</param>
    /// <returns>List of templates</returns>
    Task<IEnumerable<FineTuningTemplate>> GetTemplatesAsync(string provider, string useCase);

    /// <summary>
    /// Create fine-tuning job from template
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <param name="request">Job creation request</param>
    /// <param name="userId">User ID</param>
    /// <returns>Created job</returns>
    Task<FineTuningJob> CreateJobFromTemplateAsync(string templateId, CreateJobFromTemplateRequest request, string userId);

    /// <summary>
    /// Get fine-tuning job insights
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Job insights</returns>
    Task<FineTuningJobInsights> GetJobInsightsAsync(string jobId, string userId);

    /// <summary>
    /// Export fine-tuning data and results
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <param name="format">Export format</param>
    /// <param name="userId">User ID</param>
    /// <returns>Export data</returns>
    Task<Models.Analytics.ExportData> ExportJobDataAsync(string jobId, Models.Analytics.ExportFormat format, string userId);
}
