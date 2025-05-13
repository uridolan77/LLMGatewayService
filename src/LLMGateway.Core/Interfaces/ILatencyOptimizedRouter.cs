using LLMGateway.Core.Models.Completion;
using LLMGateway.Core.Models.Routing;

namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Interface for latency-optimized router
/// </summary>
public interface ILatencyOptimizedRouter
{
    /// <summary>
    /// Route a request based on latency optimization
    /// </summary>
    /// <param name="request">Completion request</param>
    /// <returns>Routing result</returns>
    Task<RoutingResult> RouteRequestAsync(CompletionRequest request);
}
