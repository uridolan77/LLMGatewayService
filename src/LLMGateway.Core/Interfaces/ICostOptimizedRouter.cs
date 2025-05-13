using LLMGateway.Core.Models.Completion;
using LLMGateway.Core.Models.Routing;

namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Interface for cost-optimized router
/// </summary>
public interface ICostOptimizedRouter
{
    /// <summary>
    /// Route a request based on cost optimization
    /// </summary>
    /// <param name="request">Completion request</param>
    /// <returns>Routing result</returns>
    Task<RoutingResult> RouteRequestAsync(CompletionRequest request);
}
