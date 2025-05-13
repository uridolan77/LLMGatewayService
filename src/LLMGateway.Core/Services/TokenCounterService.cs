using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Completion;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace LLMGateway.Core.Services;

/// <summary>
/// Service for counting tokens in text
/// </summary>
public class TokenCounterService : ITokenCounterService
{
    private readonly ILogger<TokenCounterService> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger">Logger</param>
    public TokenCounterService(ILogger<TokenCounterService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<int> CountTokensAsync(string text, string? modelId = null)
    {
        // Simple approximation: 1 token ~= 4 characters
        // In a real implementation, you would use a proper tokenizer based on the model
        var tokenCount = (int)Math.Ceiling(text.Length / 4.0);
        return Task.FromResult(tokenCount);
    }

    /// <inheritdoc/>
    public async Task<int> CountTokensAsync(CompletionRequest request)
    {
        int totalTokens = 0;

        // Count tokens in messages
        foreach (var message in request.Messages)
        {
            // Count tokens in role and content
            totalTokens += await CountTokensAsync(message.Role);

            if (!string.IsNullOrEmpty(message.Content))
            {
                totalTokens += await CountTokensAsync(message.Content);
            }

            // Count tokens in function call if present
            if (message.FunctionCall != null)
            {
                totalTokens += await CountTokensAsync(message.FunctionCall.Name);
                totalTokens += await CountTokensAsync(message.FunctionCall.Arguments);
            }
        }

        // Count tokens in tools if present
        if (request.Tools != null && request.Tools.Any())
        {
            var toolsJson = JsonSerializer.Serialize(request.Tools);
            totalTokens += await CountTokensAsync(toolsJson);
        }

        return totalTokens;
    }
}
