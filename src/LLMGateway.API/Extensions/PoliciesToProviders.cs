using Polly;
using Polly.Extensions.Http;
using Polly.Retry;

namespace LLMGateway.API.Extensions;

/// <summary>
/// Policies for providers
/// </summary>
public static class PoliciesToProviders
{
    /// <summary>
    /// Get retry policy
    /// </summary>
    /// <returns>Retry policy</returns>
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }
    
    /// <summary>
    /// Get circuit breaker policy
    /// </summary>
    /// <returns>Circuit breaker policy</returns>
    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
    }
}
