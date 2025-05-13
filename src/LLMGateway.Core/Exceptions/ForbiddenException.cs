namespace LLMGateway.Core.Exceptions;

/// <summary>
/// Exception thrown when a user does not have permission to access a resource
/// </summary>
public class ForbiddenException : Exception
{
    /// <summary>
    /// Constructor
    /// </summary>
    public ForbiddenException() : base("Access forbidden")
    {
    }

    /// <summary>
    /// Constructor with message
    /// </summary>
    /// <param name="message">Error message</param>
    public ForbiddenException(string message) : base(message)
    {
    }

    /// <summary>
    /// Constructor with message and inner exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public ForbiddenException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
