using LLMGateway.Core.Models.Routing;
using System;
using System.Collections.Generic;

namespace LLMGateway.Core.Options;

/// <summary>
/// Consolidated options for LLM routing
/// </summary>
public class ConsolidatedRoutingOptions
{
    /// <summary>
    /// Whether to enable dynamic routing
    /// </summary>
    public bool EnableDynamicRouting { get; init; } = true;
    
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
    /// Model mappings for routing to providers
    /// </summary>
    public List<ModelMapping> ProviderModelMappings { get; init; } = new();
    
    /// <summary>
    /// Model mappings for routing between models
    /// </summary>
    public List<ModelRouteMapping> ModelRouteMappings { get; init; } = new();
    
    /// <summary>
    /// Model routing strategies
    /// </summary>
    public List<ModelRoutingStrategy> ModelRoutingStrategies { get; init; } = new();
    
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
/// Extension methods to support migration from legacy routing options
/// </summary>
public static class RoutingOptionsExtensions
{
    /// <summary>
    /// Converts legacy LLMRoutingOptions to ConsolidatedRoutingOptions
    /// </summary>
    /// <param name="legacyOptions">Legacy LLMRoutingOptions</param>
    /// <returns>Consolidated routing options</returns>
    public static ConsolidatedRoutingOptions ToConsolidatedOptions(this LLMRoutingOptions legacyOptions)
    {
        return new ConsolidatedRoutingOptions
        {
            EnableDynamicRouting = legacyOptions.UseDynamicRouting,
            ProviderModelMappings = legacyOptions.ModelMappings
        };
    }
    
    /// <summary>
    /// Converts legacy RoutingOptions to ConsolidatedRoutingOptions
    /// </summary>
    /// <param name="legacyOptions">Legacy RoutingOptions</param>
    /// <returns>Consolidated routing options</returns>
    public static ConsolidatedRoutingOptions ToConsolidatedOptions(this RoutingOptions legacyOptions)
    {
        return new ConsolidatedRoutingOptions
        {
            EnableSmartRouting = legacyOptions.EnableSmartRouting,
            EnableLoadBalancing = legacyOptions.EnableLoadBalancing,
            EnableLatencyOptimizedRouting = legacyOptions.EnableLatencyOptimizedRouting,
            EnableCostOptimizedRouting = legacyOptions.EnableCostOptimizedRouting,
            EnableContentBasedRouting = legacyOptions.EnableContentBasedRouting,
            EnableQualityOptimizedRouting = legacyOptions.EnableQualityOptimizedRouting,
            TrackRoutingDecisions = legacyOptions.TrackRoutingDecisions,
            TrackModelMetrics = legacyOptions.TrackModelMetrics,
            EnableExperimentalRouting = legacyOptions.EnableExperimentalRouting,
            ExperimentalSamplingRate = legacyOptions.ExperimentalSamplingRate,
            ExperimentalModels = legacyOptions.ExperimentalModels,
            ModelRouteMappings = legacyOptions.ModelMappings,
            ModelRoutingStrategies = legacyOptions.ModelRoutingStrategies
        };
    }
    
    /// <summary>
    /// Merges legacy LLMRoutingOptions and RoutingOptions into ConsolidatedRoutingOptions
    /// </summary>
    /// <param name="llmRoutingOptions">Legacy LLMRoutingOptions</param>
    /// <param name="routingOptions">Legacy RoutingOptions</param>
    /// <returns>Consolidated routing options</returns>
    public static ConsolidatedRoutingOptions MergeToConsolidatedOptions(LLMRoutingOptions llmRoutingOptions, RoutingOptions routingOptions)
    {
        var consolidated = new ConsolidatedRoutingOptions
        {
            EnableDynamicRouting = llmRoutingOptions.UseDynamicRouting,
            ProviderModelMappings = llmRoutingOptions.ModelMappings,
            EnableSmartRouting = routingOptions.EnableSmartRouting,
            EnableLoadBalancing = routingOptions.EnableLoadBalancing,
            EnableLatencyOptimizedRouting = routingOptions.EnableLatencyOptimizedRouting,
            EnableCostOptimizedRouting = routingOptions.EnableCostOptimizedRouting,
            EnableContentBasedRouting = routingOptions.EnableContentBasedRouting,
            EnableQualityOptimizedRouting = routingOptions.EnableQualityOptimizedRouting,
            TrackRoutingDecisions = routingOptions.TrackRoutingDecisions,
            TrackModelMetrics = routingOptions.TrackModelMetrics,
            EnableExperimentalRouting = routingOptions.EnableExperimentalRouting,
            ExperimentalSamplingRate = routingOptions.ExperimentalSamplingRate,
            ExperimentalModels = routingOptions.ExperimentalModels,
            ModelRouteMappings = routingOptions.ModelMappings,
            ModelRoutingStrategies = routingOptions.ModelRoutingStrategies
        };
        
        return consolidated;
    }
}
