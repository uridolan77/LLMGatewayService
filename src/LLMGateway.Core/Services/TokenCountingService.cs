using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Tokenization;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace LLMGateway.Core.Services;

/// <summary>
/// Service for counting tokens in text
/// </summary>
public class TokenCountingService : ITokenCountingService
{
    private readonly ILogger<TokenCountingService> _logger;
    private readonly ILLMProviderFactory _providerFactory;
    private readonly IModelService _modelService;
    private readonly ConcurrentDictionary<string, ITokenizer> _tokenizers = new();

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger">Logger</param>
    /// <param name="providerFactory">LLM provider factory</param>
    /// <param name="modelService">Model service</param>
    public TokenCountingService(
        ILogger<TokenCountingService> logger,
        ILLMProviderFactory providerFactory,
        IModelService modelService)
    {
        _logger = logger;
        _providerFactory = providerFactory;
        _modelService = modelService;
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
            _logger.LogWarning(ex, "Failed to count tokens for model {ModelId}. Using fallback estimation.", modelId);
            return EstimateTokenCount(text);
        }
    }

    /// <inheritdoc/>
    public async Task<TokenCountEstimate> EstimateTokensAsync(Models.Completion.CompletionRequest request)
    {
        try
        {
            var modelInfo = await _modelService.GetModelAsync(request.ModelId);
            var provider = _providerFactory.GetProvider(modelInfo.Provider);

            int promptTokens = 0;

            // Count tokens in messages
            foreach (var message in request.Messages)
            {
                promptTokens += CountTokens(message.Content, request.ModelId);

                // Add overhead for message role (typically 4 tokens per message)
                promptTokens += 4;

                // Count tokens in function calls if present
                if (message.FunctionCall != null)
                {
                    promptTokens += CountTokens(message.FunctionCall.Name, request.ModelId);
                    promptTokens += CountTokens(message.FunctionCall.Arguments, request.ModelId);
                    promptTokens += 8; // Additional overhead for function call structure
                }
            }

            // Count tokens in functions if present
            if (request.Tools != null && request.Tools.Any())
            {
                foreach (var tool in request.Tools)
                {
                    if (tool.Function != null)
                    {
                        promptTokens += CountTokens(tool.Function.Name, request.ModelId);
                        promptTokens += CountTokens(tool.Function.Description, request.ModelId);
                        promptTokens += CountTokens(JsonSerializer.Serialize(tool.Function.Parameters), request.ModelId);
                        promptTokens += 10; // Additional overhead for function definition structure
                    }
                }
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
    public async Task<TokenCountEstimate> EstimateTokensAsync(Models.Embedding.EmbeddingRequest request)
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

    private ITokenizer GetTokenizerForModel(string modelId)
    {
        return _tokenizers.GetOrAdd(modelId, CreateTokenizerForModel);
    }

    private ITokenizer CreateTokenizerForModel(string modelId)
    {
        // Create the appropriate tokenizer based on the model
        return modelId.ToLowerInvariant() switch
        {
            var m when m.Contains("gpt-4") => new GPT4Tokenizer(),
            var m when m.Contains("gpt-3.5") => new GPT35Tokenizer(),
            var m when m.Contains("claude") => new ClaudeTokenizer(),
            var m when m.Contains("llama") => new LlamaTokenizer(),
            var m when m.Contains("mistral") => new MistralTokenizer(),
            var m when m.Contains("gemini") => new GeminiTokenizer(),
            _ => new DefaultTokenizer()
        };
    }

    private static int EstimateTokenCount(string text)
    {
        // Simple estimation: ~4 characters per token for English text
        return (int)Math.Ceiling(text.Length / 4.0);
    }

    private static int EstimateCompletionTokens(int promptTokens, Models.Provider.ModelInfo modelInfo)
    {
        // Default to a reasonable value if max tokens not specified
        // For chat models, typically responses are shorter than prompts
        return Math.Min(promptTokens * 2, modelInfo.ContextWindow / 2);
    }
}
