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

namespace LLMGateway.Providers.Cohere;

/// <summary>
/// Provider for Cohere
/// </summary>
public class CohereProvider : BaseLLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly CohereOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="httpClient">HTTP client</param>
    /// <param name="options">Cohere options</param>
    /// <param name="logger">Logger</param>
    public CohereProvider(
        HttpClient httpClient,
        IOptions<CohereOptions> options,
        ILogger<CohereProvider> logger)
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
    public override string Name => "Cohere";

    /// <inheritdoc/>
    public override async Task<IEnumerable<ModelInfo>> GetModelsAsync()
    {
        try
        {
            // Cohere doesn't have a list models endpoint, so we'll return a hardcoded list
            await Task.Yield(); // Make method truly async
            var models = new List<ModelInfo>
            {
                new ModelInfo
                {
                    Id = "cohere.command-r",
                    DisplayName = "Command R",
                    Provider = Name,
                    ProviderModelId = "command-r",
                    ContextWindow = 128000,
                    SupportsCompletions = true,
                    SupportsEmbeddings = false,
                    SupportsStreaming = true,
                    SupportsFunctionCalling = true,
                    SupportsVision = false
                },
                new ModelInfo
                {
                    Id = "cohere.command-r-plus",
                    DisplayName = "Command R Plus",
                    Provider = Name,
                    ProviderModelId = "command-r-plus",
                    ContextWindow = 128000,
                    SupportsCompletions = true,
                    SupportsEmbeddings = false,
                    SupportsStreaming = true,
                    SupportsFunctionCalling = true,
                    SupportsVision = false
                },
                new ModelInfo
                {
                    Id = "cohere.command-light",
                    DisplayName = "Command Light",
                    Provider = Name,
                    ProviderModelId = "command-light",
                    ContextWindow = 128000,
                    SupportsCompletions = true,
                    SupportsEmbeddings = false,
                    SupportsStreaming = true,
                    SupportsFunctionCalling = true,
                    SupportsVision = false
                },
                new ModelInfo
                {
                    Id = "cohere.embed-english-v3.0",
                    DisplayName = "Embed English v3.0",
                    Provider = Name,
                    ProviderModelId = "embed-english-v3.0",
                    ContextWindow = 512,
                    SupportsCompletions = false,
                    SupportsEmbeddings = true,
                    SupportsStreaming = false,
                    SupportsFunctionCalling = false,
                    SupportsVision = false
                },
                new ModelInfo
                {
                    Id = "cohere.embed-multilingual-v3.0",
                    DisplayName = "Embed Multilingual v3.0",
                    Provider = Name,
                    ProviderModelId = "embed-multilingual-v3.0",
                    ContextWindow = 512,
                    SupportsCompletions = false,
                    SupportsEmbeddings = true,
                    SupportsStreaming = false,
                    SupportsFunctionCalling = false,
                    SupportsVision = false
                }
            };
            
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
            if (modelId.StartsWith("cohere."))
            {
                providerModelId = modelId.Substring("cohere.".Length);
            }
            
            // Cohere doesn't have a get model endpoint, so we'll return a hardcoded model
            var models = await GetModelsAsync();
            var model = models.FirstOrDefault(m => 
                m.Id == modelId || 
                m.ProviderModelId == providerModelId);
            
            if (model == null)
            {
                throw new ProviderException(Name, $"Model {modelId} not found");
            }
            
            return model;
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
            // Convert the request to Cohere format
            var cohereRequest = ConvertToCohereChatRequest(request);
            
            // Send the request
            var response = await _httpClient.PostAsJsonAsync("/v1/chat", cohereRequest, _jsonOptions, cancellationToken);
            
            // Check for errors
            response.EnsureSuccessStatusCode();
            
            // Parse the response
            var cohereResponse = await response.Content.ReadFromJsonAsync<CohereChatResponse>(_jsonOptions, cancellationToken);
            
            if (cohereResponse == null)
            {
                throw new ProviderException(Name, "Failed to create completion: Empty response");
            }
            
            // Convert the response to the standard format
            return ConvertFromCohereChatResponse(cohereResponse, request.ModelId);
        }
        catch (Exception ex)
        {
            throw HandleProviderException(ex, "Failed to create completion");
        }
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<CompletionResponse> CreateCompletionStreamAsync(
        CompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        List<CompletionResponse> bufferedResponses = new List<CompletionResponse>();
        
        try
        {
            // Convert the request to Cohere format
            var cohereRequest = ConvertToCohereChatRequest(request);
            
            // Ensure streaming is enabled
            cohereRequest.Stream = true;
            
            // Create the HTTP request
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/chat")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(cohereRequest, _jsonOptions),
                    Encoding.UTF8,
                    "application/json")
            };
            
            // Send the request
            var response = await _httpClient.SendAsync(
                httpRequest, 
                HttpCompletionOption.ResponseHeadersRead, 
                cancellationToken);
            
            // Check for errors
            response.EnsureSuccessStatusCode();
            
            // Read the streaming response
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);
            
            string? line;
            string accumulatedText = "";
            CohereChatResponse? fullResponse = null;
            
            while ((line = await reader.ReadLineAsync()) != null)
            {
                // Skip empty lines
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                
                // Parse the SSE data
                if (line.StartsWith("data: "))
                {
                    var json = line.Substring("data: ".Length);
                    
                    // Skip [DONE] message
                    if (json == "[DONE]")
                    {
                        continue;
                    }
                    
                    CompletionResponse? standardResponse = null;
                    bool parseSuccess = false;
                    
                    try
                    {
                        var chunkResponse = JsonSerializer.Deserialize<CohereChatResponse>(json, _jsonOptions);
                        
                        if (chunkResponse != null)
                        {
                            // For the first chunk, store the full response
                            if (fullResponse == null)
                            {
                                fullResponse = chunkResponse;
                            }
                            
                            // Accumulate text for delta
                            var deltaText = chunkResponse.Text.Substring(accumulatedText.Length);
                            accumulatedText = chunkResponse.Text;
                            
                            // Create a response with just the delta
                            var deltaResponse = new CohereChatResponse
                            {
                                Text = deltaText,
                                GenerationId = chunkResponse.GenerationId,
                                FinishReason = chunkResponse.FinishReason,
                                ToolCalls = chunkResponse.ToolCalls,
                                TokenCount = chunkResponse.TokenCount
                            };
                            
                            // Convert the chunk to a standard response
                            standardResponse = ConvertFromCohereChatResponse(deltaResponse, request.ModelId, true, accumulatedText);
                            parseSuccess = true;
                        }
                    }
                    catch (JsonException ex)
                    {
                        Logger.LogWarning(ex, "Failed to parse streaming response chunk: {Json}", json);
                    }
                    
                    // Add the response to our buffer if parsing was successful
                    if (parseSuccess && standardResponse != null)
                    {
                        bufferedResponses.Add(standardResponse);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw HandleProviderException(ex, "Failed to create streaming completion");
        }
        
        // Return the buffered responses outside the try-catch block
        foreach (var response in bufferedResponses)
        {
            yield return response;
        }
    }

    /// <inheritdoc/>
    public override async Task<EmbeddingResponse> CreateEmbeddingAsync(EmbeddingRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Convert the request to Cohere format
            var cohereRequest = ConvertToCohereEmbeddingRequest(request);
            
            // Send the request
            var response = await _httpClient.PostAsJsonAsync("/v1/embed", cohereRequest, _jsonOptions, cancellationToken);
            
            // Check for errors
            response.EnsureSuccessStatusCode();
            
            // Parse the response
            var cohereResponse = await response.Content.ReadFromJsonAsync<CohereEmbeddingResponse>(_jsonOptions, cancellationToken);
            
            if (cohereResponse == null)
            {
                throw new ProviderException(Name, "Failed to create embedding: Empty response");
            }
            
            // Convert the response to the standard format
            return ConvertFromCohereEmbeddingResponse(cohereResponse, request.ModelId);
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
            // Cohere doesn't have a health check endpoint, so we'll make a simple request
            var response = await _httpClient.GetAsync("/v1/models").ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private CohereChatRequest ConvertToCohereChatRequest(CompletionRequest request)
    {
        // Extract the last user message as the main message
        var lastUserMessage = request.Messages.LastOrDefault(m => m.Role == "user");
        if (lastUserMessage == null)
        {
            throw new ProviderException(Name, "No user message found in the request");
        }
        
        // Convert the rest of the messages to chat history
        var chatHistory = new List<CohereChatMessage>();
        bool skipLastUserMessage = false;
        
        foreach (var message in request.Messages)
        {
            // Skip the last user message as it's the main message
            if (message == lastUserMessage && !skipLastUserMessage)
            {
                skipLastUserMessage = true;
                continue;
            }
            
            // Map roles
            var role = message.Role;
            if (role == "system")
            {
                role = "USER";
            }
            else if (role == "user")
            {
                role = "USER";
            }
            else if (role == "assistant")
            {
                role = "CHATBOT";
            }
            else if (role == "tool")
            {
                role = "TOOL";
            }
            
            chatHistory.Add(new CohereChatMessage
            {
                Role = role,
                Message = message.Content ?? "",
                ToolCalls = message.ToolCalls?.Select(tc => new CohereToolCall
                {
                    Id = tc.Id,
                    Name = tc.Function.Name,
                    Parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(tc.Function.Arguments) ?? new Dictionary<string, object>()
                }).ToList(),
                ToolCallId = message.ToolCallId
            });
        }
        
        return new CohereChatRequest
        {
            Model = request.ModelId,
            Message = lastUserMessage.Content ?? "",
            ChatHistory = chatHistory.Count > 0 ? chatHistory : null,
            Temperature = request.Temperature,
            P = request.TopP,
            MaxTokens = request.MaxTokens,
            Stream = request.Stream,
            Tools = request.Tools?.Select(t => new CohereTool
            {
                Name = t.Function.Name,
                Description = t.Function.Description,
                ParameterSchema = t.Function.Parameters
            }).ToList(),
            UserId = request.User
        };
    }

    private CompletionResponse ConvertFromCohereChatResponse(CohereChatResponse response, string modelId, bool isStreaming = false, string? fullText = null)
    {
        // For streaming, create a delta message
        Message? delta = null;
        if (isStreaming)
        {
            delta = new Message
            {
                Role = "assistant",
                Content = response.Text
            };
        }
        
        return new CompletionResponse
        {
            Id = response.GenerationId,
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
                        Content = fullText ?? response.Text,
                        ToolCalls = response.ToolCalls?.Select(tc => new ToolCall
                        {
                            Id = tc.Id,
                            Type = "function",
                            Function = new FunctionCall
                            {
                                Name = tc.Name,
                                Arguments = JsonSerializer.Serialize(tc.Parameters)
                            }
                        }).ToList()
                    },
                    FinishReason = response.FinishReason,
                    Delta = delta
                }
            },
            Usage = new CompletionUsage
            {
                PromptTokens = response.TokenCount?.PromptTokens ?? 0,
                CompletionTokens = response.TokenCount?.ResponseTokens ?? 0,
                TotalTokens = response.TokenCount?.TotalTokens ?? 0
            }
        };
    }

    private CohereEmbeddingRequest ConvertToCohereEmbeddingRequest(EmbeddingRequest request)
    {
        // Convert the input to a list of strings
        List<string> texts;
        
        if (request.Input is string stringInput)
        {
            texts = new List<string> { stringInput };
        }
        else if (request.Input is IEnumerable<string> stringArrayInput)
        {
            texts = stringArrayInput.ToList();
        }
        else
        {
            texts = new List<string> { JsonSerializer.Serialize(request.Input) };
        }
        
        return new CohereEmbeddingRequest
        {
            Model = request.ModelId,
            Texts = texts,
            InputType = "search_document",
            Truncate = "END"
        };
    }

    private EmbeddingResponse ConvertFromCohereEmbeddingResponse(CohereEmbeddingResponse response, string modelId)
    {
        var embeddingData = new List<EmbeddingData>();
        
        for (int i = 0; i < response.Embeddings.Count; i++)
        {
            embeddingData.Add(new EmbeddingData
            {
                Index = i,
                Object = "embedding",
                Embedding = response.Embeddings[i]
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
                PromptTokens = response.Meta?.BilledUnits?.InputTokens ?? 0,
                TotalTokens = (response.Meta?.BilledUnits?.InputTokens ?? 0) + (response.Meta?.BilledUnits?.OutputTokens ?? 0)
            }
        };
    }
}
