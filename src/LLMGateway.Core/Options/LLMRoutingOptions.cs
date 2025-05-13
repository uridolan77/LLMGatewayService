using LLMGateway.Core.Models.Routing;
using System;
using System.Collections.Generic;

namespace LLMGateway.Core.Options;

/// <summary>
/// Options for LLM routing
/// </summary>
[Obsolete("LLMRoutingOptions is deprecated. Use ConsolidatedRoutingOptions instead.")]
public class LLMRoutingOptions
{
    /// <summary>
    /// Whether to use dynamic routing
    /// </summary>
    public bool UseDynamicRouting { get; init; } = true;
    
    /// <summary>
    /// Model mappings for routing
    /// </summary>
    public List<ModelMapping> ModelMappings { get; init; } = new();
}

/// <summary>
/// Options for routing strategies
/// </summary>
[Obsolete("RoutingOptions is deprecated. Use ConsolidatedRoutingOptions instead.")]
public class RoutingOptions
{
    /// <summary>
    /// Whether to enable smart routing
    /// </summary>
    public bool EnableSmartRouting { get; init; } = true;
    
    /// <summary>
    /// Whether to enable load balancing
    /// </summary>
    public bool EnableLoadBalancing { get; init; } = true;
    
    /// <summary>
    /// Whether to enable latency-optimized routing
    /// </summary>
    public bool EnableLatencyOptimizedRouting { get; init; } = true;
    
    /// <summary>
    /// Whether to enable cost-optimized routing
    /// </summary>
    public bool EnableCostOptimizedRouting { get; init; } = true;
    
    /// <summary>
    /// Whether to enable content-based routing
    /// </summary>
    public bool EnableContentBasedRouting { get; init; } = true;
    
    /// <summary>
    /// Whether to enable quality-optimized routing
    /// </summary>
    public bool EnableQualityOptimizedRouting { get; init; } = true;
    
    /// <summary>
    /// Whether to track routing decisions
    /// </summary>
    public bool TrackRoutingDecisions { get; init; } = true;
    
    /// <summary>
    /// Whether to track model metrics
    /// </summary>
    public bool TrackModelMetrics { get; init; } = true;
    
    /// <summary>
    /// Whether to enable experimental routing
    /// </summary>
    public bool EnableExperimentalRouting { get; init; } = false;
    
    /// <summary>
    /// Sampling rate for experimental routing
    /// </summary>
    public double ExperimentalSamplingRate { get; init; } = 0.1;
    
    /// <summary>
    /// Experimental models to include in routing
    /// </summary>
    public List<string> ExperimentalModels { get; init; } = new();
    
    /// <summary>
    /// Model mappings for routing
    /// </summary>
    public List<ModelRouteMapping> ModelMappings { get; init; } = new();
    
    /// <summary>
    /// Model routing strategies
    /// </summary>
    public List<ModelRoutingStrategy> ModelRoutingStrategies { get; init; } = new();
}

/// <summary>
/// Model route mapping
/// </summary>
[Obsolete("ModelRouteMapping is being consolidated. Use ConsolidatedRoutingOptions instead.")]
public class ModelRouteMapping
{
    /// <summary>
    /// Source model ID
    /// </summary>
    public string ModelId { get; init; } = string.Empty;
    
    /// <summary>
    /// Target model ID
    /// </summary>
    public string TargetModelId { get; init; } = string.Empty;
}

/// <summary>
/// Model routing strategy
/// </summary>
[Obsolete("ModelRoutingStrategy is being consolidated. Use ConsolidatedRoutingOptions instead.")]
public class ModelRoutingStrategy
{
    /// <summary>
    /// Model ID
    /// </summary>
    public string ModelId { get; init; } = string.Empty;
    
    /// <summary>
    /// Routing strategy
    /// </summary>
    public string Strategy { get; init; } = string.Empty;
}

/// <summary>
/// User preferences for routing
/// </summary>
[Obsolete("UserPreferencesOptions is being consolidated. Use ConsolidatedRoutingOptions instead.")]
public class UserPreferencesOptions
{
    /// <summary>
    /// User routing preferences
    /// </summary>
    public List<UserRoutingPreference> UserRoutingPreferences { get; init; } = new();
    
    /// <summary>
    /// User model preferences
    /// </summary>
    public List<UserModelPreference> UserModelPreferences { get; init; } = new();
}

/// <summary>
/// User routing preference
/// </summary>
[Obsolete("UserRoutingPreference is being consolidated. Use ConsolidatedRoutingOptions instead.")]
public class UserRoutingPreference
{
    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; init; } = string.Empty;
    
    /// <summary>
    /// Routing strategy
    /// </summary>
    public string RoutingStrategy { get; init; } = string.Empty;
}

/// <summary>
/// User model preference
/// </summary>
[Obsolete("UserModelPreference is being consolidated. Use ConsolidatedRoutingOptions instead.")]
public class UserModelPreference
{
    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; init; } = string.Empty;
    
    /// <summary>
    /// Preferred model ID
    /// </summary>
    public string PreferredModelId { get; init; } = string.Empty;
}
