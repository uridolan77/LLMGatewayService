namespace LLMGateway.Infrastructure.Persistence.Entities;

/// <summary>
/// Fine-tuning file entity
/// </summary>
public class FineTuningFile
{
    /// <summary>
    /// File ID
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// File name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// File size in bytes
    /// </summary>
    public long Size { get; set; }
    
    /// <summary>
    /// File purpose
    /// </summary>
    public string Purpose { get; set; } = "fine-tune";
    
    /// <summary>
    /// Created by
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
    
    /// <summary>
    /// Created at
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Provider (e.g., OpenAI, Azure OpenAI)
    /// </summary>
    public string Provider { get; set; } = string.Empty;
    
    /// <summary>
    /// Provider-specific file ID
    /// </summary>
    public string? ProviderFileId { get; set; }
    
    /// <summary>
    /// Status
    /// </summary>
    public string Status { get; set; } = "uploaded";
    
    /// <summary>
    /// Convert to domain model
    /// </summary>
    /// <returns>Domain model</returns>
    public Core.Models.FineTuning.FineTuningFile ToDomainModel()
    {
        return new Core.Models.FineTuning.FineTuningFile
        {
            Id = Id,
            Name = Name,
            Size = Size,
            Purpose = Purpose,
            CreatedBy = CreatedBy,
            CreatedAt = CreatedAt,
            Provider = Provider,
            ProviderFileId = ProviderFileId,
            Status = Status
        };
    }
    
    /// <summary>
    /// Create from domain model
    /// </summary>
    /// <param name="model">Domain model</param>
    /// <returns>Entity</returns>
    public static FineTuningFile FromDomainModel(Core.Models.FineTuning.FineTuningFile model)
    {
        return new FineTuningFile
        {
            Id = model.Id,
            Name = model.Name,
            Size = model.Size,
            Purpose = model.Purpose,
            CreatedBy = model.CreatedBy,
            CreatedAt = model.CreatedAt,
            Provider = model.Provider,
            ProviderFileId = model.ProviderFileId,
            Status = model.Status
        };
    }
}
