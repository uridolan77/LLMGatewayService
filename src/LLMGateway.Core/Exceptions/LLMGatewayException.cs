namespace LLMGateway.Core.Exceptions;

/// <summary>
/// Base exception for all LLM Gateway exceptions
/// </summary>
public class LLMGatewayException : Exception
{
    public LLMGatewayException() : base() { }

    public LLMGatewayException(string message) : base(message) { }

    public LLMGatewayException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when validation fails
/// </summary>
public class ValidationException : LLMGatewayException
{
    /// <summary>
    /// Validation errors
    /// </summary>
    public Dictionary<string, string>? Errors { get; }

    /// <summary>
    /// Constructor with message
    /// </summary>
    /// <param name="message">Error message</param>
    public ValidationException(string message) : base(message) { }

    /// <summary>
    /// Constructor with message and inner exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public ValidationException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>
    /// Constructor with message and errors
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="errors">Validation errors</param>
    public ValidationException(string message, Dictionary<string, string> errors)
        : base(message)
    {
        Errors = errors;
    }
}

/// <summary>
/// Exception thrown when a resource is not found
/// </summary>
public class NotFoundException : LLMGatewayException
{
    public NotFoundException(string message) : base(message) { }
}

/// <summary>
/// Exception thrown when there's an issue with an LLM provider
/// </summary>
public class ProviderException : LLMGatewayException
{
    public string ProviderName { get; }
    public string? ErrorCode { get; }

    public ProviderException(string providerName, string message)
        : base(message)
    {
        ProviderName = providerName;
    }

    public ProviderException(string providerName, string message, string errorCode)
        : base(message)
    {
        ProviderName = providerName;
        ErrorCode = errorCode;
    }

    public ProviderException(string providerName, string message, Exception innerException)
        : base(message, innerException)
    {
        ProviderName = providerName;
    }

    public ProviderException(string providerName, string message, string errorCode, Exception innerException)
        : base(message, innerException)
    {
        ProviderName = providerName;
        ErrorCode = errorCode;
    }
}

/// <summary>
/// Exception thrown when rate limits are exceeded
/// </summary>
public class RateLimitExceededException : ProviderException
{
    public RateLimitExceededException(string providerName, string message)
        : base(providerName, message, "rate_limit_exceeded") { }
}

/// <summary>
/// Exception thrown when authentication fails with a provider
/// </summary>
public class ProviderAuthenticationException : ProviderException
{
    public ProviderAuthenticationException(string providerName, string message)
        : base(providerName, message, "authentication_error") { }
}

/// <summary>
/// Exception thrown when a provider is unavailable
/// </summary>
public class ProviderUnavailableException : ProviderException
{
    public ProviderUnavailableException(string providerName, string message)
        : base(providerName, message, "provider_unavailable") { }
}

/// <summary>
/// Exception thrown when a model is not found
/// </summary>
public class ModelNotFoundException : NotFoundException
{
    public string ModelId { get; }

    public ModelNotFoundException(string modelId)
        : base($"Model '{modelId}' not found")
    {
        ModelId = modelId;
    }
}

/// <summary>
/// Exception thrown when a provider is not found
/// </summary>
public class ProviderNotFoundException : NotFoundException
{
    public string ProviderName { get; }

    public ProviderNotFoundException(string providerName)
        : base($"Provider '{providerName}' not found")
    {
        ProviderName = providerName;
    }
}

/// <summary>
/// Exception thrown when routing fails
/// </summary>
public class RoutingException : LLMGatewayException
{
    public RoutingException(string message) : base(message) { }

    public RoutingException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when all fallback attempts fail
/// </summary>
public class FallbackExhaustedException : LLMGatewayException
{
    public FallbackExhaustedException(string message) : base(message) { }

    public FallbackExhaustedException(string message, Exception innerException)
        : base(message, innerException) { }
}
