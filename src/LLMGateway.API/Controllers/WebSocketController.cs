using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using LLMGateway.Core.Commands;
using LLMGateway.Core.Models.Completion;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LLMGateway.API.Controllers;

/// <summary>
/// Controller for WebSocket connections
/// </summary>
[ApiController]
[Route("api/v1/ws")]
[Authorize(Policy = "CompletionAccess")]
public class WebSocketController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<WebSocketController> _logger;

    public WebSocketController(
        IMediator mediator,
        ILogger<WebSocketController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Handle WebSocket connections for real-time streaming
    /// </summary>
    /// <returns>WebSocket connection</returns>
    [HttpGet]
    public async Task HandleWebSocket()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await HandleWebSocketConnection(webSocket);
        }
        else
        {
            HttpContext.Response.StatusCode = 400;
            await HttpContext.Response.WriteAsync("WebSocket connection required");
        }
    }

    private async Task HandleWebSocketConnection(WebSocket webSocket)
    {
        var connectionId = Guid.NewGuid().ToString();
        var userId = User.Identity?.Name;
        var apiKey = GetApiKeyFromHeaders();

        _logger.LogInformation("WebSocket connection established: {ConnectionId} for user {UserId}", connectionId, userId);

        try
        {
            var buffer = new byte[1024 * 4];
            var receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            while (!receiveResult.CloseStatus.HasValue)
            {
                try
                {
                    // Parse the incoming message
                    var message = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
                    _logger.LogDebug("Received WebSocket message: {Message}", message);

                    var request = JsonSerializer.Deserialize<WebSocketRequest>(message);
                    if (request == null)
                    {
                        await SendErrorAsync(webSocket, "Invalid request format", connectionId);
                        continue;
                    }

                    // Handle different message types
                    switch (request.Type?.ToLowerInvariant())
                    {
                        case "completion":
                            await HandleCompletionRequest(webSocket, request, userId, apiKey, connectionId);
                            break;

                        case "ping":
                            await SendPongAsync(webSocket, connectionId);
                            break;

                        default:
                            await SendErrorAsync(webSocket, $"Unknown message type: {request.Type}", connectionId);
                            break;
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse WebSocket message");
                    await SendErrorAsync(webSocket, "Invalid JSON format", connectionId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing WebSocket message");
                    await SendErrorAsync(webSocket, "Internal server error", connectionId);
                }

                // Receive next message
                receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }

            _logger.LogInformation("WebSocket connection closed: {ConnectionId}", connectionId);
        }
        catch (WebSocketException ex)
        {
            _logger.LogWarning(ex, "WebSocket connection error: {ConnectionId}", connectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in WebSocket connection: {ConnectionId}", connectionId);
        }
        finally
        {
            if (webSocket.State == WebSocketState.Open)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None);
            }
        }
    }

    private async Task HandleCompletionRequest(WebSocket webSocket, WebSocketRequest request, string? userId, string? apiKey, string connectionId)
    {
        try
        {
            if (request.Data == null)
            {
                await SendErrorAsync(webSocket, "Missing completion request data", connectionId);
                return;
            }

            // Parse completion request from the data
            var completionRequest = JsonSerializer.Deserialize<CompletionRequest>(request.Data.ToString()!);
            if (completionRequest == null)
            {
                await SendErrorAsync(webSocket, "Invalid completion request format", connectionId);
                return;
            }

            _logger.LogInformation("Processing WebSocket completion request for model {ModelId}", completionRequest.ModelId);

            // Send acknowledgment
            await SendMessageAsync(webSocket, new WebSocketResponse
            {
                Type = "completion_started",
                RequestId = request.RequestId,
                Data = new { model = completionRequest.ModelId }
            });

            // Create streaming completion command
            var command = new CreateStreamingCompletionCommand(completionRequest, userId, apiKey, CancellationToken.None);
            var responseStream = await _mediator.Send(command, CancellationToken.None);

            // Stream the response chunks
            await foreach (var chunk in responseStream)
            {
                if (webSocket.State != WebSocketState.Open)
                {
                    break;
                }

                await SendMessageAsync(webSocket, new WebSocketResponse
                {
                    Type = "completion_chunk",
                    RequestId = request.RequestId,
                    Data = chunk
                });
            }

            // Send completion finished message
            await SendMessageAsync(webSocket, new WebSocketResponse
            {
                Type = "completion_finished",
                RequestId = request.RequestId,
                Data = new { status = "completed" }
            });

            _logger.LogInformation("WebSocket completion request completed for model {ModelId}", completionRequest.ModelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling WebSocket completion request");
            await SendErrorAsync(webSocket, "Failed to process completion request", connectionId, request.RequestId);
        }
    }

    private async Task SendPongAsync(WebSocket webSocket, string connectionId)
    {
        await SendMessageAsync(webSocket, new WebSocketResponse
        {
            Type = "pong",
            Data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
        });
    }

    private async Task SendErrorAsync(WebSocket webSocket, string error, string connectionId, string? requestId = null)
    {
        _logger.LogWarning("Sending WebSocket error: {Error} for connection {ConnectionId}", error, connectionId);

        await SendMessageAsync(webSocket, new WebSocketResponse
        {
            Type = "error",
            RequestId = requestId,
            Data = new { error = error, timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
        });
    }

    private async Task SendMessageAsync(WebSocket webSocket, WebSocketResponse response)
    {
        if (webSocket.State != WebSocketState.Open)
        {
            return;
        }

        var json = JsonSerializer.Serialize(response);
        var bytes = Encoding.UTF8.GetBytes(json);
        await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private string? GetApiKeyFromHeaders()
    {
        return Request.Headers.TryGetValue("X-API-Key", out var apiKey) ? apiKey.FirstOrDefault() : null;
    }
}

/// <summary>
/// WebSocket request model
/// </summary>
public class WebSocketRequest
{
    /// <summary>
    /// Message type
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Request ID for tracking
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Request data
    /// </summary>
    public object? Data { get; set; }
}

/// <summary>
/// WebSocket response model
/// </summary>
public class WebSocketResponse
{
    /// <summary>
    /// Message type
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Request ID for tracking
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Response data
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// Timestamp
    /// </summary>
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
}
