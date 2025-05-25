using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
// using TikToken; // Commented out for now due to package issues
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Completion;
using LLMGateway.Core.Models.Embedding;
using LLMGateway.Core.Models.TokenUsage;
using LLMGateway.Core.Models.Provider;
using LLMGateway.Core.Models.Tokenization;
using LLMGateway.Core.Services.Tokenizers;

namespace LLMGateway.Core.Services;

/// <summary>
/// Enhanced token counting service using TikToken for accurate tokenization
/// </summary>
public class TiktokenTokenCountingService : ITokenCountingService
{
    private readonly ILogger<TiktokenTokenCountingService> _logger;
    private readonly IModelService _modelService;
    private readonly ILLMProviderFactory _providerFactory;
    private readonly ConcurrentDictionary<string, LLMGateway.Core.Interfaces.ITokenizer> _tokenizers = new();

    public TiktokenTokenCountingService(
        ILogger<TiktokenTokenCountingService> logger,
        IModelService modelService,
        ILLMProviderFactory providerFactory)
    {
        _logger = logger;
        _modelService = modelService;
        _providerFactory = providerFactory;
    }

    /// <inheritdoc/>
    public int CountTokens(string text, string modelId)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        try
        {
            var tokenizer = GetTokenizerForModel(modelId);
            return tokenizer.CountTokens(text);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to count tokens for model {ModelId} using enhanced tokenizer. Using fallback estimation.", modelId);
            return EstimateTokenCount(text);
        }
    }

    /// <inheritdoc/>
    public async Task<TokenCountEstimate> EstimateTokensAsync(CompletionRequest request)
    {
        try
        {
            var modelInfo = await _modelService.GetModelAsync(request.ModelId);

            int promptTokens = 0;

            // Count tokens in messages
            if (request.Messages != null && request.Messages.Any())
            {
                foreach (var message in request.Messages)
                {
                    if (!string.IsNullOrEmpty(message.Content))
                    {
                        promptTokens += CountTokens(message.Content, request.ModelId);

                        // Add overhead for message formatting (role, etc.)
                        promptTokens += GetMessageOverhead(request.ModelId);
                    }
                }
            }

            // Add system message overhead if present
            if (request.Messages?.Any(m => m.Role == "system") == true)
            {
                promptTokens += GetSystemMessageOverhead(request.ModelId);
            }

            // Estimate completion tokens based on max_tokens
            int completionTokens = request.MaxTokens ?? EstimateCompletionTokens(promptTokens, modelInfo);

            return new TokenCountEstimate
            {
                PromptTokens = promptTokens,
                EstimatedCompletionTokens = completionTokens,
                TotalTokens = promptTokens + completionTokens,
                ModelId = request.ModelId,
                Provider = modelInfo.Provider
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to estimate tokens for completion request with model {ModelId}", request.ModelId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<TokenCountEstimate> EstimateTokensAsync(EmbeddingRequest request)
    {
        try
        {
            var modelInfo = await _modelService.GetModelAsync(request.ModelId);

            int promptTokens = 0;

            // Count tokens in input
            if (request.Input is string stringInput)
            {
                promptTokens = CountTokens(stringInput, request.ModelId);
            }
            else if (request.Input is IEnumerable<string> stringArrayInput)
            {
                foreach (var input in stringArrayInput)
                {
                    promptTokens += CountTokens(input, request.ModelId);
                }
            }
            else
            {
                // Fallback for other input types
                var serialized = JsonSerializer.Serialize(request.Input);
                promptTokens = CountTokens(serialized, request.ModelId);
            }

            return new TokenCountEstimate
            {
                PromptTokens = promptTokens,
                EstimatedCompletionTokens = 0, // Embeddings don't have completion tokens
                TotalTokens = promptTokens,
                ModelId = request.ModelId,
                Provider = modelInfo.Provider
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to estimate tokens for embedding request with model {ModelId}", request.ModelId);
            throw;
        }
    }

    private LLMGateway.Core.Interfaces.ITokenizer GetTokenizerForModel(string modelId)
    {
        return _tokenizers.GetOrAdd(modelId, id =>
        {
            return CreateTokenizerForModel(id);
        });
    }

    private LLMGateway.Core.Interfaces.ITokenizer CreateTokenizerForModel(string modelId)
    {
        var lowerModelId = modelId.ToLowerInvariant();

        // Use model-specific tokenizers based on the model family
        if (lowerModelId.Contains("gpt-4"))
        {
            return new LLMGateway.Core.Services.Tokenizers.GPT4Tokenizer();
        }
        else if (lowerModelId.Contains("gpt-3.5") || lowerModelId.Contains("gpt-35"))
        {
            return new LLMGateway.Core.Services.Tokenizers.GPT35Tokenizer();
        }
        else if (lowerModelId.Contains("claude"))
        {
            return new LLMGateway.Core.Services.Tokenizers.ClaudeTokenizer();
        }
        else if (lowerModelId.Contains("llama"))
        {
            return new LLMGateway.Core.Services.Tokenizers.LlamaTokenizer();
        }
        else
        {
            // Default tokenizer for unknown models
            return new LLMGateway.Core.Services.Tokenizers.DefaultTokenizer();
        }
    }



    private int GetMessageOverhead(string modelId)
    {
        // Different models have different overhead for message formatting
        var lowerModelId = modelId.ToLowerInvariant();

        if (lowerModelId.Contains("gpt-4") || lowerModelId.Contains("gpt-3.5-turbo"))
        {
            return 4; // Overhead for role and message formatting
        }

        return 2; // Conservative estimate for other models
    }

    private int GetSystemMessageOverhead(string modelId)
    {
        // Additional overhead for system messages
        return 3;
    }

    private int EstimateCompletionTokens(int promptTokens, ModelInfo modelInfo)
    {
        // Estimate completion tokens based on prompt length and model characteristics
        // This is a heuristic and can be improved with historical data

        var maxTokens = modelInfo.ContextWindow > 0 ? modelInfo.ContextWindow : 4096; // Default context window

        if (promptTokens < 100)
        {
            return Math.Min(150, maxTokens / 4);
        }
        else if (promptTokens < 500)
        {
            return Math.Min(300, maxTokens / 3);
        }
        else
        {
            return Math.Min(500, maxTokens / 2);
        }
    }

    private int EstimateTokenCount(string text)
    {
        // Fallback estimation: approximately 4 characters per token
        return (int)Math.Ceiling(text.Length / 4.0);
    }
}
