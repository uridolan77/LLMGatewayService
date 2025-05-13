using Polly.Retry;

namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Interface for retry policy service
/// </summary>
public interface IRetryPolicyService
{
    /// <summary>
    /// Create an async retry policy
    /// </summary>
    /// <param name="operationName">Name of the operation</param>
    /// <returns>Async retry policy</returns>
    AsyncRetryPolicy CreateAsyncRetryPolicy(string operationName);
    
    /// <summary>
    /// Create an async retry policy with a specific return type
    /// </summary>
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="operationName">Name of the operation</param>
    /// <returns>Async retry policy</returns>
    AsyncRetryPolicy<T> CreateAsyncRetryPolicy<T>(string operationName);
    
    /// <summary>
    /// Create an async retry policy for a provider
    /// </summary>
    /// <param name="providerName">Name of the provider</param>
    /// <returns>Async retry policy</returns>
    AsyncRetryPolicy CreateProviderRetryPolicy(string providerName);
}
