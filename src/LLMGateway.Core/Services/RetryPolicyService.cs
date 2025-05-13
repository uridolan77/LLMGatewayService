using LLMGateway.Core.Exceptions;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using System.Net.Http;

namespace LLMGateway.Core.Services;

/// <summary>
/// Service for creating retry policies
/// </summary>
public class RetryPolicyService : IRetryPolicyService
{
    private readonly ILogger<RetryPolicyService> _logger;
    private readonly RetryPolicyOptions _options;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger">Logger</param>
    /// <param name="options">Retry policy options</param>
    public RetryPolicyService(
        ILogger<RetryPolicyService> logger,
        IOptions<RetryPolicyOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc/>
    public AsyncRetryPolicy CreateAsyncRetryPolicy(string operationName)
    {
        return Policy
            .Handle<HttpRequestException>(ex => IsTransientHttpException(ex))
            .Or<ProviderUnavailableException>()
            .Or<TaskCanceledException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                _options.MaxRetryAttempts,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt) * _options.BaseRetryIntervalSeconds),
                (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        exception,
                        "Retry {RetryCount} of {MaxRetryCount} for {OperationName} after {RetryInterval}s delay due to: {ErrorMessage}",
                        retryCount,
                        _options.MaxRetryAttempts,
                        operationName,
                        timeSpan.TotalSeconds,
                        exception.Message);
                });
    }

    /// <inheritdoc/>
    public AsyncRetryPolicy<T> CreateAsyncRetryPolicy<T>(string operationName)
    {
        return Policy<T>
            .Handle<HttpRequestException>(ex => IsTransientHttpException(ex))
            .Or<ProviderUnavailableException>()
            .Or<TaskCanceledException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                _options.MaxRetryAttempts,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt) * _options.BaseRetryIntervalSeconds),
                (outcome, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        outcome.Exception,
                        "Retry {RetryCount} of {MaxRetryCount} for {OperationName} after {RetryInterval}s delay due to: {ErrorMessage}",
                        retryCount,
                        _options.MaxRetryAttempts,
                        operationName,
                        timeSpan.TotalSeconds,
                        outcome.Exception?.Message ?? "Unknown error");
                });
    }

    /// <inheritdoc/>
    public AsyncRetryPolicy CreateProviderRetryPolicy(string providerName)
    {
        return Policy
            .Handle<HttpRequestException>(ex => IsTransientHttpException(ex))
            .Or<ProviderUnavailableException>()
            .Or<TaskCanceledException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                _options.MaxProviderRetryAttempts,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt) * _options.BaseRetryIntervalSeconds),
                (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        exception,
                        "Provider retry {RetryCount} of {MaxRetryCount} for {ProviderName} after {RetryInterval}s delay due to: {ErrorMessage}",
                        retryCount,
                        _options.MaxProviderRetryAttempts,
                        providerName,
                        timeSpan.TotalSeconds,
                        exception.Message);
                });
    }

    private static bool IsTransientHttpException(HttpRequestException ex)
    {
        // Consider these status codes as transient errors that can be retried
        return ex.StatusCode switch
        {
            System.Net.HttpStatusCode.RequestTimeout => true,
            System.Net.HttpStatusCode.TooManyRequests => true,
            System.Net.HttpStatusCode.InternalServerError => true,
            System.Net.HttpStatusCode.BadGateway => true,
            System.Net.HttpStatusCode.ServiceUnavailable => true,
            System.Net.HttpStatusCode.GatewayTimeout => true,
            _ => false
        };
    }
}
