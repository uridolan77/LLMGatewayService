namespace LLMGateway.Infrastructure.Persistence.Entities;

/// <summary>
/// Fine-tuning file content entity
/// </summary>
public class FineTuningFileContent
{
    /// <summary>
    /// File ID
    /// </summary>
    public string FileId { get; set; } = string.Empty;
    
    /// <summary>
    /// File content
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Navigation property for file
    /// </summary>
    public virtual FineTuningFile File { get; set; } = null!;
}
