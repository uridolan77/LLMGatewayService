using LLMGateway.Core.Exceptions;
using LLMGateway.Core.Models.Completion;
using LLMGateway.Core.Models.Embedding;
using LLMGateway.Core.Models.Provider;
using LLMGateway.Providers.Base;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LLMGateway.Providers.HuggingFace;

/// <summary>
/// Provider for HuggingFace
/// </summary>
public class HuggingFaceProvider : BaseLLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly HuggingFaceOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly Dictionary<string, string> _modelEndpoints = new()
    {
        { "mistralai/Mistral-7B-Instruct-v0.2", "text-generation" },
        { "meta-llama/Llama-2-7b-chat-hf", "text-generation" },
        { "meta-llama/Llama-2-13b-chat-hf", "text-generation" },
        { "meta-llama/Llama-2-70b-chat-hf", "text-generation" },
        { "tiiuae/falcon-7b-instruct", "text-generation" },
        { "tiiuae/falcon-40b-instruct", "text-generation" },
        { "HuggingFaceH4/zephyr-7b-beta", "text-generation" },
        { "google/flan-t5-xxl", "text-generation" },
        { "google/flan-ul2", "text-generation" },
        { "sentence-transformers/all-MiniLM-L6-v2", "feature-extraction" },
        { "sentence-transformers/all-mpnet-base-v2", "feature-extraction" },
        { "BAAI/bge-large-en-v1.5", "feature-extraction" }
    };

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="httpClient">HTTP client</param>
    /// <param name="options">HuggingFace options</param>
    /// <param name="logger">Logger</param>
    public HuggingFaceProvider(
        HttpClient httpClient,
        IOptions<HuggingFaceOptions> options,
        ILogger<HuggingFaceProvider> logger)
        : base(logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        
        // Configure the HTTP client
        _httpClient.BaseAddress = new Uri(_options.ApiUrl);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        
        // Configure JSON options
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
    }

    /// <inheritdoc/>
    public override string Name => "HuggingFace";

    /// <inheritdoc/>
    public override async Task<IEnumerable<ModelInfo>> GetModelsAsync()
    {
        try
        {
            // HuggingFace doesn't have a list models endpoint for inference API, so we'll return a hardcoded list
            await Task.Delay(0).ConfigureAwait(false); // Make method truly async
            var models = new List<ModelInfo>();
            
            foreach (var model in _modelEndpoints)
            {
                var modelId = model.Key;
                var endpoint = model.Value;
                
                models.Add(new ModelInfo
                {
                    Id = $"huggingface.{modelId.Replace('/', '_')}",
                    DisplayName = modelId,
                    Provider = Name,
                    ProviderModelId = modelId,
                    ContextWindow = GetContextWindowForModel(modelId),
                    SupportsCompletions = endpoint == "text-generation",
                    SupportsEmbeddings = endpoint == "feature-extraction",
                    SupportsStreaming = false,
                    SupportsFunctionCalling = false,
                    SupportsVision = false
                });
            }
            
            return models;
        }
        catch (Exception ex)
        {
            throw HandleProviderException(ex, "Failed to get models");
        }
    }

    /// <inheritdoc/>
    public override async Task<ModelInfo> GetModelAsync(string modelId)
    {
        try
        {
            // Strip the provider prefix if present
            var providerModelId = modelId;
            if (modelId.StartsWith("huggingface."))
            {
                providerModelId = modelId.Substring("huggingface.".Length).Replace('_', '/');
            }
            
            // Check if the model is in our hardcoded list
            if (!_modelEndpoints.TryGetValue(providerModelId, out var endpoint))
            {
                throw new ProviderException(Name, $"Model {modelId} not found");
            }
            
            await Task.Delay(0).ConfigureAwait(false); // Make method truly async
            
            return new ModelInfo
            {
                Id = $"huggingface.{providerModelId.Replace('/', '_')}",
                DisplayName = providerModelId,
                Provider = Name,
                ProviderModelId = providerModelId,
                ContextWindow = GetContextWindowForModel(providerModelId),
                SupportsCompletions = endpoint == "text-generation",
                SupportsEmbeddings = endpoint == "feature-extraction",
                SupportsStreaming = false,
                SupportsFunctionCalling = false,
                SupportsVision = false
            };
        }
        catch (Exception ex)
        {
            throw HandleProviderException(ex, $"Failed to get model {modelId}");
        }
    }

    /// <inheritdoc/>
    public override async Task<CompletionResponse> CreateCompletionAsync(CompletionRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get the model endpoint
            if (!_modelEndpoints.TryGetValue(request.ModelId, out var endpoint))
            {
                throw new ProviderException(Name, $"Model {request.ModelId} not found");
            }
            
            if (endpoint != "text-generation")
            {
                throw new ProviderException(Name, $"Model {request.ModelId} does not support text generation");
            }
            
            // Convert the request to HuggingFace format
            var huggingFaceRequest = ConvertToHuggingFaceTextGenerationRequest(request);
            
            // Send the request
            var response = await _httpClient.PostAsJsonAsync($"/{request.ModelId}", huggingFaceRequest, _jsonOptions, cancellationToken).ConfigureAwait(false);
            
            // Check for errors
            response.EnsureSuccessStatusCode();
            
            // Parse the response
            var huggingFaceResponse = await response.Content.ReadFromJsonAsync<HuggingFaceTextGenerationResponse[]>(_jsonOptions, cancellationToken).ConfigureAwait(false);
            
            if (huggingFaceResponse == null || huggingFaceResponse.Length == 0)
            {
                throw new ProviderException(Name, "Failed to create completion: Empty response");
            }
            
            // Convert the response to the standard format
            return ConvertFromHuggingFaceTextGenerationResponse(huggingFaceResponse[0], request.ModelId);
        }
        catch (Exception ex)
        {
            throw HandleProviderException(ex, "Failed to create completion");
        }
    }

    /// <inheritdoc/>
    public override IAsyncEnumerable<CompletionResponse> CreateCompletionStreamAsync(
        CompletionRequest request, 
        CancellationToken cancellationToken = default)
    {
        // HuggingFace Inference API doesn't support streaming, so we'll use the default implementation
        return CreateDefaultCompletionStreamAsync(request, cancellationToken);
    }

    /// <inheritdoc/>
    public override async Task<EmbeddingResponse> CreateEmbeddingAsync(EmbeddingRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get the model endpoint
            if (!_modelEndpoints.TryGetValue(request.ModelId, out var endpoint))
            {
                throw new ProviderException(Name, $"Model {request.ModelId} not found");
            }
            
            if (endpoint != "feature-extraction")
            {
                throw new ProviderException(Name, $"Model {request.ModelId} does not support feature extraction");
            }
            
            // Convert the request to HuggingFace format
            var huggingFaceRequest = ConvertToHuggingFaceFeatureExtractionRequest(request);
            
            // Send the request
            var response = await _httpClient.PostAsJsonAsync($"/{request.ModelId}", huggingFaceRequest, _jsonOptions, cancellationToken).ConfigureAwait(false);
            
            // Check for errors
            response.EnsureSuccessStatusCode();
            
            // Parse the response
            var huggingFaceResponse = await response.Content.ReadFromJsonAsync<List<List<float>>>(_jsonOptions, cancellationToken).ConfigureAwait(false);
            
            if (huggingFaceResponse == null || huggingFaceResponse.Count == 0)
            {
                throw new ProviderException(Name, "Failed to create embedding: Empty response");
            }
            
            // Convert the response to the standard format
            return ConvertFromHuggingFaceFeatureExtractionResponse(huggingFaceResponse, request.ModelId);
        }
        catch (Exception ex)
        {
            throw HandleProviderException(ex, "Failed to create embedding");
        }
    }

    /// <inheritdoc/>
    public override async Task<bool> IsAvailableAsync()
    {
        try
        {
            // HuggingFace doesn't have a health check endpoint, so we'll make a simple request
            var response = await _httpClient.GetAsync("/status").ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private HuggingFaceTextGenerationRequest ConvertToHuggingFaceTextGenerationRequest(CompletionRequest request)
    {
        // Convert messages to a prompt
        var prompt = "";
        
        foreach (var message in request.Messages)
        {
            if (message.Role == "system")
            {
                prompt += $"<|system|>\n{message.Content}\n";
            }
            else if (message.Role == "user")
            {
                prompt += $"<|user|>\n{message.Content}\n";
            }
            else if (message.Role == "assistant")
            {
                prompt += $"<|assistant|>\n{message.Content}\n";
            }
        }
        
        // Add the assistant prompt for the response
        prompt += "<|assistant|>\n";
        
        return new HuggingFaceTextGenerationRequest
        {
            Inputs = prompt,
            Parameters = new HuggingFaceTextGenerationParameters
            {
                Temperature = request.Temperature,
                TopP = request.TopP,
                MaxNewTokens = request.MaxTokens,
                ReturnFullText = false,
                DoSample = request.Temperature > 0
            },
            Options = new HuggingFaceTextGenerationOptions
            {
                UseCache = true,
                WaitForModel = true
            }
        };
    }

    private CompletionResponse ConvertFromHuggingFaceTextGenerationResponse(HuggingFaceTextGenerationResponse response, string modelId)
    {
        return new CompletionResponse
        {
            Id = Guid.NewGuid().ToString(),
            Object = "chat.completion",
            Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Model = modelId,
            Provider = Name,
            Choices = new List<CompletionChoice>
            {
                new CompletionChoice
                {
                    Index = 0,
                    Message = new Message
                    {
                        Role = "assistant",
                        Content = response.GeneratedText
                    },
                    FinishReason = response.Details?.FinishReason ?? "stop"
                }
            },
            Usage = new CompletionUsage
            {
                PromptTokens = response.Details?.PromptTokens ?? 0,
                CompletionTokens = response.Details?.GeneratedTokens ?? response.GeneratedTokens ?? 0,
                TotalTokens = (response.Details?.PromptTokens ?? 0) + (response.Details?.GeneratedTokens ?? response.GeneratedTokens ?? 0)
            }
        };
    }

    private HuggingFaceFeatureExtractionRequest ConvertToHuggingFaceFeatureExtractionRequest(EmbeddingRequest request)
    {
        return new HuggingFaceFeatureExtractionRequest
        {
            Inputs = request.Input,
            Options = new HuggingFaceFeatureExtractionOptions
            {
                UseCache = true,
                WaitForModel = true
            }
        };
    }

    private EmbeddingResponse ConvertFromHuggingFaceFeatureExtractionResponse(List<List<float>> response, string modelId)
    {
        var embeddingData = new List<EmbeddingData>();
        
        for (int i = 0; i < response.Count; i++)
        {
            embeddingData.Add(new EmbeddingData
            {
                Index = i,
                Object = "embedding",
                Embedding = response[i]
            });
        }
        
        return new EmbeddingResponse
        {
            Object = "list",
            Model = modelId,
            Provider = Name,
            Data = embeddingData,
            Usage = new EmbeddingUsage
            {
                PromptTokens = 0, // HuggingFace doesn't provide token usage
                TotalTokens = 0
            }
        };
    }

    private int GetContextWindowForModel(string modelId)
    {
        // Return the context window size for the model
        return modelId switch
        {
            "mistralai/Mistral-7B-Instruct-v0.2" => 8192,
            "meta-llama/Llama-2-7b-chat-hf" => 4096,
            "meta-llama/Llama-2-13b-chat-hf" => 4096,
            "meta-llama/Llama-2-70b-chat-hf" => 4096,
            "tiiuae/falcon-7b-instruct" => 2048,
            "tiiuae/falcon-40b-instruct" => 2048,
            "HuggingFaceH4/zephyr-7b-beta" => 4096,
            "google/flan-t5-xxl" => 512,
            "google/flan-ul2" => 512,
            "sentence-transformers/all-MiniLM-L6-v2" => 256,
            "sentence-transformers/all-mpnet-base-v2" => 384,
            "BAAI/bge-large-en-v1.5" => 512,
            _ => 1024
        };
    }
}
