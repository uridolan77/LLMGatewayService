using MediatR;
using LLMGateway.Core.Models.Completion;

namespace LLMGateway.Core.Commands;

/// <summary>
/// Command to create a completion
/// </summary>
public class CreateCompletionCommand : IRequest<CompletionResponse>
{
    /// <summary>
    /// The completion request
    /// </summary>
    public CompletionRequest Request { get; }

    /// <summary>
    /// The user making the request
    /// </summary>
    public string? UserId { get; }

    /// <summary>
    /// Request ID for tracking
    /// </summary>
    public string RequestId { get; }

    /// <summary>
    /// API key used for the request
    /// </summary>
    public string? ApiKey { get; }

    /// <summary>
    /// Whether to use streaming
    /// </summary>
    public bool Stream { get; }

    public CreateCompletionCommand(
        CompletionRequest request, 
        string? userId = null, 
        string? apiKey = null, 
        bool stream = false)
    {
        Request = request ?? throw new ArgumentNullException(nameof(request));
        UserId = userId;
        ApiKey = apiKey;
        Stream = stream;
        RequestId = Guid.NewGuid().ToString();
    }
}

/// <summary>
/// Command to create a streaming completion
/// </summary>
public class CreateStreamingCompletionCommand : IRequest<IAsyncEnumerable<CompletionChunk>>
{
    /// <summary>
    /// The completion request
    /// </summary>
    public CompletionRequest Request { get; }

    /// <summary>
    /// The user making the request
    /// </summary>
    public string? UserId { get; }

    /// <summary>
    /// Request ID for tracking
    /// </summary>
    public string RequestId { get; }

    /// <summary>
    /// API key used for the request
    /// </summary>
    public string? ApiKey { get; }

    /// <summary>
    /// Cancellation token
    /// </summary>
    public CancellationToken CancellationToken { get; }

    public CreateStreamingCompletionCommand(
        CompletionRequest request, 
        string? userId = null, 
        string? apiKey = null,
        CancellationToken cancellationToken = default)
    {
        Request = request ?? throw new ArgumentNullException(nameof(request));
        UserId = userId;
        ApiKey = apiKey;
        RequestId = Guid.NewGuid().ToString();
        CancellationToken = cancellationToken;
    }
}

/// <summary>
/// Command to create a batch of completions
/// </summary>
public class CreateBatchCompletionCommand : IRequest<BatchCompletionResponse>
{
    /// <summary>
    /// The batch completion request
    /// </summary>
    public BatchCompletionRequest Request { get; }

    /// <summary>
    /// The user making the request
    /// </summary>
    public string? UserId { get; }

    /// <summary>
    /// Request ID for tracking
    /// </summary>
    public string RequestId { get; }

    /// <summary>
    /// API key used for the request
    /// </summary>
    public string? ApiKey { get; }

    public CreateBatchCompletionCommand(
        BatchCompletionRequest request, 
        string? userId = null, 
        string? apiKey = null)
    {
        Request = request ?? throw new ArgumentNullException(nameof(request));
        UserId = userId;
        ApiKey = apiKey;
        RequestId = Guid.NewGuid().ToString();
    }
}

/// <summary>
/// Batch completion request model
/// </summary>
public class BatchCompletionRequest
{
    /// <summary>
    /// List of completion requests
    /// </summary>
    public List<CompletionRequest> Requests { get; set; } = new();

    /// <summary>
    /// Maximum number of concurrent requests
    /// </summary>
    public int MaxConcurrency { get; set; } = 5;

    /// <summary>
    /// Whether to fail fast on first error
    /// </summary>
    public bool FailFast { get; set; } = false;
}

/// <summary>
/// Batch completion response model
/// </summary>
public class BatchCompletionResponse
{
    /// <summary>
    /// List of completion responses
    /// </summary>
    public List<CompletionResponse> Responses { get; set; } = new();

    /// <summary>
    /// Number of successful completions
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of failed completions
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Total processing time in milliseconds
    /// </summary>
    public double TotalDurationMs { get; set; }

    /// <summary>
    /// Any errors that occurred during batch processing
    /// </summary>
    public List<string> Errors { get; set; } = new();
}
