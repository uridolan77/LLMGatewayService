namespace LLMGateway.Core.Models.Cost;

/// <summary>
/// Cost management options
/// </summary>
public class CostManagementOptions
{
    /// <summary>
    /// Default pricing for models
    /// </summary>
    public Dictionary<string, Dictionary<string, ModelPricing>> DefaultPricing { get; set; } = new();
    
    /// <summary>
    /// Fine-tuning pricing for models
    /// </summary>
    public Dictionary<string, Dictionary<string, decimal>> FineTuningPricing { get; set; } = new();
    
    /// <summary>
    /// Fallback input price per token (per 1000 tokens)
    /// </summary>
    public decimal FallbackInputPricePerToken { get; set; } = 0.01m;
    
    /// <summary>
    /// Fallback output price per token (per 1000 tokens)
    /// </summary>
    public decimal FallbackOutputPricePerToken { get; set; } = 0.02m;
    
    /// <summary>
    /// Fallback fine-tuning price per token (per 1000 tokens)
    /// </summary>
    public decimal FallbackFineTuningPricePerToken { get; set; } = 0.03m;
    
    /// <summary>
    /// Enable cost tracking
    /// </summary>
    public bool EnableCostTracking { get; set; } = true;
    
    /// <summary>
    /// Enable budget enforcement
    /// </summary>
    public bool EnableBudgetEnforcement { get; set; } = true;
}

/// <summary>
/// Model pricing
/// </summary>
public class ModelPricing
{
    /// <summary>
    /// Input price per token (per 1000 tokens)
    /// </summary>
    public decimal InputPricePerToken { get; set; }
    
    /// <summary>
    /// Output price per token (per 1000 tokens)
    /// </summary>
    public decimal OutputPricePerToken { get; set; }
}
