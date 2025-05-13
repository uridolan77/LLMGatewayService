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

namespace LLMGateway.Providers.OpenAI;

/// <summary>
/// Provider for OpenAI
/// </summary>
public class OpenAIProvider : BaseLLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly OpenAIOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="httpClient">HTTP client</param>
    /// <param name="options">OpenAI options</param>
    /// <param name="logger">Logger</param>
    public OpenAIProvider(
        HttpClient httpClient,
        IOptions<OpenAIOptions> options,
        ILogger<OpenAIProvider> logger)
        : base(logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        
        // Configure the HTTP client
        _httpClient.BaseAddress = new Uri(_options.ApiUrl);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        
        if (!string.IsNullOrEmpty(_options.OrganizationId))
        {
            _httpClient.DefaultRequestHeaders.Add("OpenAI-Organization", _options.OrganizationId);
        }
        
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
    public override string Name => "OpenAI";

    /// <inheritdoc/>
    public override async Task<IEnumerable<ModelInfo>> GetModelsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<OpenAIListModelsResponse>("/models", _jsonOptions).ConfigureAwait(false);
            
            if (response == null || response.Data == null)
            {
                throw new ProviderException(Name, "Failed to get models: Empty response");
            }
            
            return response.Data.Select(m => new ModelInfo
            {
                Id = $"openai.{m.Id}",
                DisplayName = m.Id,
                Provider = Name,
                ProviderModelId = m.Id,
                ContextWindow = GetContextWindowForModel(m.Id),
                SupportsCompletions = true,
                SupportsEmbeddings = m.Id.StartsWith("text-embedding"),
                SupportsStreaming = true,
                SupportsFunctionCalling = m.Id.Contains("gpt-4") || m.Id.Contains("gpt-3.5"),
                SupportsVision = m.Id.Contains("vision")
            });
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
            if (modelId.StartsWith("openai."))
            {
                providerModelId = modelId.Substring("openai.".Length);
            }
            
            var response = await _httpClient.GetFromJsonAsync<OpenAIModel>($"/models/{providerModelId}", _jsonOptions).ConfigureAwait(false);
            
            if (response == null)
            {
                throw new ProviderException(Name, $"Failed to get model {modelId}: Empty response");
            }
            
            return new ModelInfo
            {
                Id = $"openai.{response.Id}",
                DisplayName = response.Id,
                Provider = Name,
                ProviderModelId = response.Id,
                ContextWindow = GetContextWindowForModel(response.Id),
                SupportsCompletions = true,
                SupportsEmbeddings = response.Id.StartsWith("text-embedding"),
                SupportsStreaming = true,
                SupportsFunctionCalling = response.Id.Contains("gpt-4") || response.Id.Contains("gpt-3.5"),
                SupportsVision = response.Id.Contains("vision")
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
            // Convert the request to OpenAI format
            var openAIRequest = ConvertToOpenAICompletionRequest(request);
            
            // Send the request
            var response = await _httpClient.PostAsJsonAsync("/chat/completions", openAIRequest, _jsonOptions, cancellationToken).ConfigureAwait(false);
            
            // Check for errors
            response.EnsureSuccessStatusCode();
            
            // Parse the response
            var openAIResponse = await response.Content.ReadFromJsonAsync<OpenAIChatCompletionResponse>(_jsonOptions, cancellationToken).ConfigureAwait(false);
            
            if (openAIResponse == null)
            {
                throw new ProviderException(Name, "Failed to create completion: Empty response");
            }
            
            // Convert the response to the standard format
            return ConvertFromOpenAICompletionResponse(openAIResponse);
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
            // Convert the request to OpenAI format
            var openAIRequest = ConvertToOpenAICompletionRequest(request);
            
            // Ensure streaming is enabled
            openAIRequest.Stream = true;
            
            // Create the HTTP request
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/chat/completions")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(openAIRequest, _jsonOptions),
                    Encoding.UTF8,
                    "application/json")
            };
            
            // Send the request
            var response = await _httpClient.SendAsync(
                httpRequest, 
                HttpCompletionOption.ResponseHeadersRead, 
                cancellationToken).ConfigureAwait(false);
            
            // Check for errors
            response.EnsureSuccessStatusCode();
            
            // Read the streaming response
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var reader = new StreamReader(stream);
            
            string? line;
            while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
            {
                // Skip empty lines and "data: [DONE]"
                if (string.IsNullOrWhiteSpace(line) || line == "data: [DONE]")
                {
                    continue;
                }
                
                // Parse the SSE data
                if (line.StartsWith("data: "))
                {
                    var json = line.Substring("data: ".Length);
                    
                    try
                    {
                        var chunkResponse = JsonSerializer.Deserialize<OpenAIChatCompletionResponse>(json, _jsonOptions);
                        
                        if (chunkResponse != null)
                        {
                            var standardResponse = ConvertFromOpenAICompletionResponse(chunkResponse);
                            bufferedResponses.Add(standardResponse);
                        }
                    }
                    catch (JsonException ex)
                    {
                        Logger.LogWarning(ex, "Failed to parse streaming response chunk: {Json}", json);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw HandleProviderException(ex, "Failed to create streaming completion");
        }
        
        // Return all buffered responses outside of the try-catch block
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
            // Convert the request to OpenAI format
            var openAIRequest = ConvertToOpenAIEmbeddingRequest(request);
            
            // Send the request
            var response = await _httpClient.PostAsJsonAsync("/embeddings", openAIRequest, _jsonOptions, cancellationToken);
            
            // Check for errors
            response.EnsureSuccessStatusCode();
            
            // Parse the response
            var openAIResponse = await response.Content.ReadFromJsonAsync<OpenAIEmbeddingResponse>(_jsonOptions, cancellationToken);
            
            if (openAIResponse == null)
            {
                throw new ProviderException(Name, "Failed to create embedding: Empty response");
            }
            
            // Convert the response to the standard format
            return ConvertFromOpenAIEmbeddingResponse(openAIResponse);
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
            var response = await _httpClient.GetAsync("/models").ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private OpenAIChatCompletionRequest ConvertToOpenAICompletionRequest(CompletionRequest request)
    {
        return new OpenAIChatCompletionRequest
        {
            Model = request.ModelId,
            Messages = request.Messages.Select(m => new OpenAIChatMessage
            {
                Role = m.Role,
                Content = m.Content,
                Name = m.Name,
                ToolCalls = m.ToolCalls?.Select(tc => new OpenAIToolCall
                {
                    Id = tc.Id,
                    Type = tc.Type,
                    Function = new OpenAIFunctionCall
                    {
                        Name = tc.Function.Name,
                        Arguments = tc.Function.Arguments
                    }
                }).ToList(),
                ToolCallId = m.ToolCallId
            }).ToList(),
            MaxTokens = request.MaxTokens,
            Temperature = request.Temperature,
            TopP = request.TopP,
            N = request.N,
            Stream = request.Stream,
            Stop = request.Stop,
            PresencePenalty = request.PresencePenalty,
            FrequencyPenalty = request.FrequencyPenalty,
            LogitBias = request.LogitBias,
            User = request.User,
            ResponseFormat = request.ResponseFormat != null ? new OpenAIResponseFormat
            {
                Type = request.ResponseFormat.Type
            } : null,
            Tools = request.Tools?.Select(t => new OpenAITool
            {
                Type = t.Type,
                Function = new OpenAIFunctionDefinition
                {
                    Name = t.Function.Name,
                    Description = t.Function.Description,
                    Parameters = t.Function.Parameters
                }
            }).ToList(),
            ToolChoice = request.ToolChoice != null ? (request.ToolChoice.Type == "function" ? new OpenAIToolChoice
            {
                Type = request.ToolChoice.Type,
                Function = new OpenAIFunctionChoice
                {
                    Name = request.ToolChoice.Function?.Name ?? string.Empty
                }
            } : new OpenAIToolChoice
            {
                Type = request.ToolChoice.Type
            }) : null
        };
    }

    private CompletionResponse ConvertFromOpenAICompletionResponse(OpenAIChatCompletionResponse response)
    {
        return new CompletionResponse
        {
            Id = response.Id,
            Object = response.Object,
            Created = response.Created,
            Model = response.Model,
            Provider = Name,
            Choices = response.Choices.Select(c => new CompletionChoice
            {
                Index = c.Index,
                Message = c.Message != null ? new Message
                {
                    Role = c.Message.Role,
                    Content = c.Message.Content,
                    Name = c.Message.Name,
                    ToolCalls = c.Message.ToolCalls?.Select(tc => new ToolCall
                    {
                        Id = tc.Id,
                        Type = tc.Type,
                        Function = new FunctionCall
                        {
                            Name = tc.Function.Name,
                            Arguments = tc.Function.Arguments
                        }
                    }).ToList(),
                    ToolCallId = c.Message.ToolCallId
                } : new Message(),
                FinishReason = c.FinishReason,
                Delta = c.Delta != null ? new Message
                {
                    Role = c.Delta.Role,
                    Content = c.Delta.Content,
                    Name = c.Delta.Name,
                    ToolCalls = c.Delta.ToolCalls?.Select(tc => new ToolCall
                    {
                        Id = tc.Id,
                        Type = tc.Type,
                        Function = new FunctionCall
                        {
                            Name = tc.Function.Name,
                            Arguments = tc.Function.Arguments
                        }
                    }).ToList(),
                    ToolCallId = c.Delta.ToolCallId
                } : null
            }).ToList(),
            Usage = response.Usage != null ? new CompletionUsage
            {
                PromptTokens = response.Usage.PromptTokens,
                CompletionTokens = response.Usage.CompletionTokens,
                TotalTokens = response.Usage.TotalTokens
            } : new CompletionUsage(),
            SystemFingerprint = response.SystemFingerprint
        };
    }

    private OpenAIEmbeddingRequest ConvertToOpenAIEmbeddingRequest(EmbeddingRequest request)
    {
        return new OpenAIEmbeddingRequest
        {
            Model = request.ModelId,
            Input = request.Input,
            User = request.User,
            Dimensions = request.Dimensions,
            EncodingFormat = request.EncodingFormat
        };
    }

    private EmbeddingResponse ConvertFromOpenAIEmbeddingResponse(OpenAIEmbeddingResponse response)
    {
        return new EmbeddingResponse
        {
            Object = response.Object,
            Model = response.Model,
            Provider = Name,
            Data = response.Data.Select((d, i) => new EmbeddingData
            {
                Index = i,
                Object = d.Object,
                Embedding = d.Embedding
            }).ToList(),
            Usage = new EmbeddingUsage
            {
                PromptTokens = response.Usage.PromptTokens,
                TotalTokens = response.Usage.TotalTokens
            }
        };
    }

    private int GetContextWindowForModel(string modelId)
    {
        // Return the context window size for the model
        return modelId switch
        {
            "gpt-4-turbo" => 128000,
            "gpt-4-vision-preview" => 128000,
            "gpt-4" => 8192,
            "gpt-4-32k" => 32768,
            "gpt-3.5-turbo" => 16384,
            "gpt-3.5-turbo-16k" => 16384,
            _ => 4096
        };
    }
}
