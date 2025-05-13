using LLMGateway.Core.Models.Completion;
using LLMGateway.Core.Models.Routing;

namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Interface for content-based router
/// </summary>
public interface IContentBasedRouter
{
    /// <summary>
    /// Route a request based on its content
    /// </summary>
    /// <param name="request">Completion request</param>
    /// <returns>Routing result</returns>
    Task<RoutingResult> RouteRequestAsync(CompletionRequest request);
}
