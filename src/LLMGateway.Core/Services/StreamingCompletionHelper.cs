using LLMGateway.Core.Exceptions;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Completion;
using Microsoft.Extensions.Logging;

namespace LLMGateway.Core.Services;

/// <summary>
/// Helper class for streaming completions
/// </summary>
internal static class StreamingCompletionHelper
{    /// <summary>
    /// Process a streaming completion with fallback support
    /// </summary>
    public static async IAsyncEnumerable<CompletionResponse> ProcessStreamWithFallbacksAsync(
        IModelRouter modelRouter,
        ILLMProviderFactory providerFactory,
        ITokenUsageService tokenUsageService,
        RoutingResult initialRoutingResult,
        CompletionRequest request,
        string originalModelId,
        bool enableFallbacks,
        int maxFallbackAttempts,
        bool trackTokenUsage,
        ILogger logger,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        int attemptCount = 0;
        Exception? lastException = null;
        RoutingResult routingResult = initialRoutingResult;
        string requestModelId = request.ModelId;
        ILLMProvider provider = providerFactory.GetProvider(routingResult.Provider);
        request.ModelId = routingResult.ProviderModelId;

        while (attemptCount <= maxFallbackAttempts)
        {
            attemptCount++;

            // First, try to create the stream outside of any yield context
            IAsyncEnumerable<CompletionResponse>? responseStream = null;
            bool streamCreated = false;
            
            try
            {
                responseStream = provider.CreateCompletionStreamAsync(request, cancellationToken);
                streamCreated = true;
            }
            catch (ProviderException ex) when (enableFallbacks && attemptCount <= maxFallbackAttempts)
            {
                lastException = ex;
                logger.LogWarning(ex, "Provider {Provider} failed to create streaming completion for model {ModelId}. Attempting fallback.", 
                    routingResult.Provider, routingResult.ModelId);

                // Get fallback models
                var fallbackModels = await modelRouter.GetFallbackModelsAsync(originalModelId, ex.ErrorCode);
                if (!fallbackModels.Any())
                {
                    logger.LogWarning("No fallback models available for {ModelId}", originalModelId);
                    throw new FallbackExhaustedException($"No fallback models available for {originalModelId}", ex);
                }

                // Try the next fallback model
                var fallbackModelId = fallbackModels.ElementAt(Math.Min(attemptCount - 1, fallbackModels.Count() - 1));
                request.ModelId = fallbackModelId;

                // Route the request to the fallback model
                routingResult = await modelRouter.RouteCompletionRequestAsync(request);
                if (!routingResult.Success)
                {
                    throw new RoutingException(routingResult.ErrorMessage ?? 
                        "Failed to route completion request to fallback model", ex);
                }

                // Get the provider for the fallback model
                provider = providerFactory.GetProvider(routingResult.Provider);
                request.ModelId = routingResult.ProviderModelId;

                logger.LogInformation("Falling back to model {ModelId} with provider {Provider}", 
                    fallbackModelId, routingResult.Provider);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create streaming completion for model {ModelId} with provider {Provider}", 
                    request.ModelId, routingResult.Provider);
                lastException = ex;
                throw;
            }
            
            // If we successfully created a stream, process it
            if (streamCreated && responseStream != null)
            {
                bool streamingSuccess = true;
                List<CompletionResponse> bufferedResponses = new List<CompletionResponse>();
                
                try
                {
                    // Process the stream but buffer responses instead of yielding them directly
                    await foreach (var response in responseStream.WithCancellation(cancellationToken))
                    {
                        // Set the provider and model in the response
                        response.Provider = routingResult.Provider;
                        response.Model = originalModelId;

                        // Track token usage for the first chunk only
                        if (trackTokenUsage && response.Choices.Any(c => c.Delta == null || c.Delta.Content == null))
                        {
                            await tokenUsageService.TrackCompletionTokenUsageAsync(request, response);
                        }
                        
                        bufferedResponses.Add(response);
                    }
                }
                catch (Exception ex)
                {
                    streamingSuccess = false;
                    lastException = ex;
                    logger.LogError(ex, "Error processing streaming response for model {ModelId} with provider {Provider}", 
                        routingResult.ModelId, routingResult.Provider);
                    
                    if (!enableFallbacks || attemptCount > maxFallbackAttempts)
                    {
                        throw;
                    }
                    
                    // If we're allowing fallbacks, continue to try the next provider
                    continue;
                }
                
                // If streaming was successful, yield all buffered responses
                if (streamingSuccess)
                {
                    foreach (var response in bufferedResponses)
                    {
                        yield return response;
                    }
                    
                    // We're done
                    yield break;
                }
            }
        }

        // If we get here, all fallback attempts failed
        if (lastException != null)
        {
            throw new FallbackExhaustedException($"All fallback attempts failed for model {originalModelId}", 
                lastException);
        }
        else
        {
            throw new FallbackExhaustedException($"All fallback attempts failed for model {originalModelId}");
        }
    }
}
