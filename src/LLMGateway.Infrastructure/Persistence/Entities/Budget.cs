using System.Text.Json;
using LLMGateway.Core.Models.Cost;

namespace LLMGateway.Infrastructure.Persistence.Entities;

/// <summary>
/// Budget entity
/// </summary>
public class Budget
{
    /// <summary>
    /// Budget ID
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Budget name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Budget description
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Project ID
    /// </summary>
    public string? ProjectId { get; set; }
    
    /// <summary>
    /// Amount in USD
    /// </summary>
    public decimal AmountUsd { get; set; }
    
    /// <summary>
    /// Start date
    /// </summary>
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// End date
    /// </summary>
    public DateTime? EndDate { get; set; }
    
    /// <summary>
    /// Reset period
    /// </summary>
    public BudgetResetPeriod ResetPeriod { get; set; } = BudgetResetPeriod.Never;
    
    /// <summary>
    /// Alert threshold percentage
    /// </summary>
    public int AlertThresholdPercentage { get; set; } = 80;
    
    /// <summary>
    /// Whether to enforce the budget
    /// </summary>
    public bool EnforceBudget { get; set; } = false;
    
    /// <summary>
    /// Created at
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Updated at
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Tags (stored as JSON)
    /// </summary>
    public string TagsJson { get; set; } = "[]";
    
    /// <summary>
    /// Get tags
    /// </summary>
    /// <returns>Tags</returns>
    public List<string> GetTags()
    {
        if (string.IsNullOrEmpty(TagsJson))
        {
            return new List<string>();
        }
        
        try
        {
            return JsonSerializer.Deserialize<List<string>>(TagsJson) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
    
    /// <summary>
    /// Set tags
    /// </summary>
    /// <param name="tags">Tags</param>
    public void SetTags(List<string> tags)
    {
        TagsJson = JsonSerializer.Serialize(tags);
    }
    
    /// <summary>
    /// Convert to domain model
    /// </summary>
    /// <returns>Domain model</returns>
    public Core.Models.Cost.Budget ToDomainModel()
    {
        return new Core.Models.Cost.Budget
        {
            Id = Id,
            Name = Name,
            Description = Description,
            UserId = UserId,
            ProjectId = ProjectId,
            AmountUsd = AmountUsd,
            StartDate = StartDate,
            EndDate = EndDate,
            ResetPeriod = ResetPeriod,
            AlertThresholdPercentage = AlertThresholdPercentage,
            EnforceBudget = EnforceBudget,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            Tags = GetTags()
        };
    }
    
    /// <summary>
    /// Create from domain model
    /// </summary>
    /// <param name="model">Domain model</param>
    /// <returns>Entity</returns>
    public static Budget FromDomainModel(Core.Models.Cost.Budget model)
    {
        var entity = new Budget
        {
            Id = model.Id,
            Name = model.Name,
            Description = model.Description,
            UserId = model.UserId,
            ProjectId = model.ProjectId,
            AmountUsd = model.AmountUsd,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            ResetPeriod = model.ResetPeriod,
            AlertThresholdPercentage = model.AlertThresholdPercentage,
            EnforceBudget = model.EnforceBudget,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt
        };
        
        entity.SetTags(model.Tags);
        
        return entity;
    }
}
