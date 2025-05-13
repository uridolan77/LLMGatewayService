# Routing Options Consolidation Proposal

## Current State

The LLM Gateway currently has two separate option classes for routing:

1. **`LLMRoutingOptions`** - Used for basic model-to-provider mappings
   - Contains `UseDynamicRouting` flag
   - Contains `ModelMappings` which maps model IDs to provider names

2. **`RoutingOptions`** - Used for advanced routing strategies
   - Contains flags for different routing strategies (cost-optimized, latency-optimized, etc.)
   - Contains experimental routing configurations
   - Contains model-specific routing strategy mappings

## Confusion and Issues

This dual approach creates several problems:

1. **Cognitive Overhead**: Developers need to understand which routing option class to use for which purpose
2. **Configuration Complexity**: Settings are split across different config sections
3. **Dependency Injection**: Some services need both option types injected
4. **Semantic Clarity**: The distinction between the two isn't immediately obvious from their names

## Proposed Solution

### Option 1: Consolidate into a Single Class

```csharp
public class RoutingOptions
{
    // Core settings
    public bool EnableDynamicRouting { get; init; } = true;
    
    // Strategy flags
    public bool EnableSmartRouting { get; init; } = true;
    public bool EnableLoadBalancing { get; init; } = true;
    public bool EnableLatencyOptimizedRouting { get; init; } = true;
    public bool EnableCostOptimizedRouting { get; init; } = true;
    public bool EnableContentBasedRouting { get; init; } = true;
    public bool EnableQualityOptimizedRouting { get; init; } = true;
    
    // Monitoring/tracking
    public bool TrackRoutingDecisions { get; init; } = true;
    public bool TrackModelMetrics { get; init; } = true;
    
    // Experimental settings
    public bool EnableExperimentalRouting { get; init; } = false;
    public double ExperimentalSamplingRate { get; init; } = 0.1;
    public List<string> ExperimentalModels { get; init; } = new();
    
    // Mappings
    public List<ModelMapping> ProviderModelMappings { get; init; } = new();
    public List<ModelRouteMapping> ModelRouteMappings { get; init; } = new();
    public List<ModelRoutingStrategy> ModelRoutingStrategies { get; init; } = new();
}
```

### Option 2: Rename for Clarity

If consolidation isn't possible due to breaking changes, rename the classes for clarity:

- Rename `LLMRoutingOptions` to `ProviderMappingOptions`
- Keep `RoutingOptions` as is

### Option 3: Create Hierarchical Options

```csharp
public class RoutingOptions
{
    public ProviderMappingOptions ProviderMapping { get; init; } = new();
    public RoutingStrategyOptions RoutingStrategies { get; init; } = new();
    public ExperimentalRoutingOptions Experimental { get; init; } = new();
    public UserRoutingOptions UserPreferences { get; init; } = new();
}
```

## Implementation Plan

1. Create new consolidated options class
2. Add [Obsolete] attributes to old classes
3. Update service registrations to use the new class
4. Update consumers to use the new class
5. Eventually remove the old classes

## Migration Support

For backward compatibility during migration, create extension methods to convert between the old and new formats.

## Recommended Approach

**Option 1** provides the cleanest solution moving forward while maintaining all functionality. This consolidation will improve code clarity and simplify configuration.
