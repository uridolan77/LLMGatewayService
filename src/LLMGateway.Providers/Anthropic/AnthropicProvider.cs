using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LLMGateway.Core.Exceptions;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Completion;
using LLMGateway.Core.Models.Embedding;
using LLMGateway.Core.Models.Provider;
using LLMGateway.Providers.Base;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LLMGateway.Providers.Anthropic;

/// <summary>
/// Enhanced Anthropic provider with integrated Phase 1 and Phase 2 capabilities
/// </summary>
public class AnthropicProvider : BaseLLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AnthropicOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Constructor with enhanced services
    /// </summary>
    /// <param name="httpClient">HTTP client</param>
    /// <param name="httpClientFactory">HTTP client factory</param>
    /// <param name="options">Anthropic options</param>
    /// <param name="logger">Logger</param>
    /// <param name="circuitBreakerService">Circuit breaker service</param>
    /// <param name="tokenCountingService">Token counting service</param>
    /// <param name="cacheService">Enhanced cache service</param>
    /// <param name="contentFilteringService">Content filtering service</param>
    /// <param name="metricsService">Metrics service</param>
    public AnthropicProvider(
        HttpClient httpClient,
        IHttpClientFactory httpClientFactory,
        IOptions<AnthropicOptions> options,
        ILogger<AnthropicProvider> logger,
        ICircuitBreakerService circuitBreakerService,
        ITokenCountingService tokenCountingService,
        IEnhancedCacheService cacheService,
        IContentFilteringService contentFilteringService,
        IMetricsService metricsService)
        : base(logger, circuitBreakerService, tokenCountingService, cacheService, contentFilteringService, metricsService)
    {
        _httpClient = httpClient;
        _httpClientFactory = httpClientFactory;
        _options = options.Value;

        // Configure the HTTP client
        _httpClient.BaseAddress = new Uri(_options.ApiUrl);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", _options.ApiVersion);
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
    public override string Name => "Anthropic";

    /// <inheritdoc/>
    public override async Task<IEnumerable<ModelInfo>> GetModelsAsync()
    {
        try
        {
            // Anthropic doesn't have a list models endpoint, so we'll return a hardcoded list
            await Task.Yield(); // Make method truly async
            var models = new List<ModelInfo>
            {
                new ModelInfo
                {
                    Id = "anthropic.claude-3-opus",
                    DisplayName = "Claude 3 Opus",
                    Provider = Name,
                    ProviderModelId = "claude-3-opus-20240229",
                    ContextWindow = 200000,
                    SupportsCompletions = true,
                    SupportsEmbeddings = false,
                    SupportsStreaming = true,
                    SupportsFunctionCalling = true,
                    SupportsVision = true
                },
                new ModelInfo
                {
                    Id = "anthropic.claude-3-sonnet",
                    DisplayName = "Claude 3 Sonnet",
                    Provider = Name,
                    ProviderModelId = "claude-3-sonnet-20240229",
                    ContextWindow = 200000,
                    SupportsCompletions = true,
                    SupportsEmbeddings = false,
                    SupportsStreaming = true,
                    SupportsFunctionCalling = true,
                    SupportsVision = true
                },
                new ModelInfo
                {
                    Id = "anthropic.claude-3-haiku",
                    DisplayName = "Claude 3 Haiku",
                    Provider = Name,
                    ProviderModelId = "claude-3-haiku-20240307",
                    ContextWindow = 200000,
                    SupportsCompletions = true,
                    SupportsEmbeddings = false,
                    SupportsStreaming = true,
                    SupportsFunctionCalling = true,
                    SupportsVision = true
                },
                new ModelInfo
                {
                    Id = "anthropic.claude-2.1",
                    DisplayName = "Claude 2.1",
                    Provider = Name,
                    ProviderModelId = "claude-2.1",
                    ContextWindow = 200000,
                    SupportsCompletions = true,
                    SupportsEmbeddings = false,
                    SupportsStreaming = true,
                    SupportsFunctionCalling = false,
                    SupportsVision = false
                },
                new ModelInfo
                {
                    Id = "anthropic.claude-2.0",
                    DisplayName = "Claude 2.0",
                    Provider = Name,
                    ProviderModelId = "claude-2.0",
                    ContextWindow = 100000,
                    SupportsCompletions = true,
                    SupportsEmbeddings = false,
                    SupportsStreaming = true,
                    SupportsFunctionCalling = false,
                    SupportsVision = false
                },
                new ModelInfo
                {
                    Id = "anthropic.claude-instant-1.2",
                    DisplayName = "Claude Instant 1.2",
                    Provider = Name,
                    ProviderModelId = "claude-instant-1.2",
                    ContextWindow = 100000,
                    SupportsCompletions = true,
                    SupportsEmbeddings = false,
                    SupportsStreaming = true,
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
            if (modelId.StartsWith("anthropic."))
            {
                providerModelId = modelId.Substring("anthropic.".Length);
            }

            // Anthropic doesn't have a get model endpoint, so we'll return a hardcoded model
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
        return await CreateEnhancedCompletionAsync(request, cancellationToken);
    }

    /// <inheritdoc/>
    protected override async Task<CompletionResponse> CreateCompletionInternalAsync(CompletionRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = new Activity("Anthropic.CreateCompletionInternal").Start();
        activity?.SetTag("model", request.ModelId);

        try
        {
            Logger.LogDebug("Creating Anthropic completion for model {ModelId}", request.ModelId);

            // Convert the request to Anthropic format
            var anthropicRequest = ConvertToAnthropicMessageRequest(request);

            // Send the request
            var response = await _httpClient.PostAsJsonAsync("/v1/messages", anthropicRequest, _jsonOptions, cancellationToken).ConfigureAwait(false);

            // Check for errors
            response.EnsureSuccessStatusCode();

            // Parse the response
            var anthropicResponse = await response.Content.ReadFromJsonAsync<AnthropicMessageResponse>(_jsonOptions, cancellationToken).ConfigureAwait(false);

            if (anthropicResponse == null)
            {
                throw new ProviderException(Name, "Failed to create completion: Empty response");
            }

            // Convert the response to the standard format
            var standardResponse = ConvertFromAnthropicMessageResponse(anthropicResponse);

            Logger.LogDebug("Anthropic completion created successfully for model {ModelId}", request.ModelId);
            return standardResponse;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Anthropic completion failed for model {ModelId}", request.ModelId);
            throw HandleProviderException(ex, "Failed to create completion");
        }
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<CompletionResponse> CreateCompletionStreamAsync(
        CompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var response in CreateEnhancedCompletionStreamAsync(request, cancellationToken))
        {
            yield return response;
        }
    }

    /// <inheritdoc/>
    protected override async IAsyncEnumerable<CompletionResponse> CreateCompletionStreamInternalAsync(
        CompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        AnthropicMessageResponse? fullResponse = null;
        List<CompletionResponse> bufferedResponses = new List<CompletionResponse>();

        // Create a client with appropriate timeout
        using var httpClient = _httpClientFactory.CreateClient(Name);

        try
        {
            // Convert the request to Anthropic format
            var anthropicRequest = ConvertToAnthropicMessageRequest(request);

            // Ensure stream is true
            anthropicRequest.Stream = true;

            // Create the request message
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, _options.MessagesEndpoint)
            {
                Content = JsonContent.Create(anthropicRequest, MediaTypeHeaderValue.Parse("application/json"), _jsonOptions)
            };

            // Add headers
            httpRequestMessage.Headers.Add("x-api-key", _options.ApiKey);
            httpRequestMessage.Headers.Add("anthropic-version", _options.ApiVersion);

            // Send the request
            using var httpResponseMessage = await httpClient.SendAsync(
                httpRequestMessage,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);

            // Ensure success
            httpResponseMessage.EnsureSuccessStatusCode();

            // Get the response stream
            using var responseStream = await httpResponseMessage.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var reader = new StreamReader(responseStream);

            // Process the SSE stream
            string? line;

            while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
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

                    try
                    {
                        var chunkResponse = JsonSerializer.Deserialize<AnthropicMessageResponse>(json, _jsonOptions);

                        if (chunkResponse != null)
                        {
                            // For the first chunk, store the full response
                            if (fullResponse == null)
                            {
                                fullResponse = chunkResponse;
                            }

                            // Convert the chunk to a standard response
                            var standardResponse = ConvertFromAnthropicMessageResponse(chunkResponse, fullResponse);

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

        // Return all the collected responses outside the try-catch block
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
            // Anthropic doesn't have an embeddings endpoint yet, so we'll throw an exception
            await Task.Yield(); // Make method truly async
            throw new ProviderException(Name, "Embeddings are not supported by Anthropic");
        }
        catch (Exception ex)
        {
            throw HandleProviderException(ex, "Failed to create embedding");
        }
    }

    /// <inheritdoc/>
    public override async Task<bool> IsAvailableAsync()
    {
        using var activity = new Activity("Anthropic.HealthCheck").Start();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            Logger.LogDebug("Checking Anthropic provider availability");

            // Anthropic doesn't have a health check endpoint, so we'll make a simple request
            var response = await _httpClient.GetAsync("/v1/models").ConfigureAwait(false);
            var isHealthy = response.IsSuccessStatusCode;

            stopwatch.Stop();
            MetricsService.RecordProviderHealth(Name, isHealthy, stopwatch.Elapsed.TotalMilliseconds);

            Logger.LogDebug("Anthropic provider health check completed: {IsHealthy} ({Duration}ms)",
                isHealthy, stopwatch.Elapsed.TotalMilliseconds);

            return isHealthy;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            MetricsService.RecordProviderHealth(Name, false, stopwatch.Elapsed.TotalMilliseconds);

            Logger.LogWarning(ex, "Anthropic provider health check failed ({Duration}ms)",
                stopwatch.Elapsed.TotalMilliseconds);

            return false;
        }
    }

    private AnthropicMessageRequest ConvertToAnthropicMessageRequest(CompletionRequest request)
    {
        // Extract system message if present
        string? systemMessage = null;
        var messages = new List<AnthropicMessage>();

        foreach (var message in request.Messages)
        {
            if (message.Role == "system")
            {
                systemMessage = message.Content;
            }
            else
            {
                // Convert content to the appropriate format
                object content;

                if (message.Content != null)
                {
                    content = message.Content;
                }
                else
                {
                    content = new List<AnthropicContentBlock>();
                }

                messages.Add(new AnthropicMessage
                {
                    Role = message.Role,
                    Content = content,
                    ToolCalls = message.ToolCalls?.Select(tc => new AnthropicToolCall
                    {
                        Id = tc.Id,
                        Type = tc.Type,
                        Function = new AnthropicFunctionCall
                        {
                            Name = tc.Function.Name,
                            Arguments = tc.Function.Arguments
                        }
                    }).ToList()
                });
            }
        }

        return new AnthropicMessageRequest
        {
            Model = request.ModelId,
            Messages = messages,
            System = systemMessage,
            MaxTokens = request.MaxTokens ?? 4096,
            Temperature = request.Temperature,
            TopP = request.TopP,
            Stream = request.Stream,
            StopSequences = request.Stop,
            Tools = request.Tools?.Select(t => new AnthropicTool
            {
                Type = t.Type,
                Function = new AnthropicFunctionDefinition
                {
                    Name = t.Function.Name,
                    Description = t.Function.Description,
                    Parameters = t.Function.Parameters
                }
            }).ToList(),
            ToolChoice = request.ToolChoice != null ? new AnthropicToolChoice
            {
                Type = request.ToolChoice.Type,
                Function = request.ToolChoice.Function != null ? new AnthropicFunctionChoice
                {
                    Name = request.ToolChoice.Function.Name
                } : null
            } : null
        };
    }

    private CompletionResponse ConvertFromAnthropicMessageResponse(AnthropicMessageResponse response, AnthropicMessageResponse? fullResponse = null)
    {
        // Extract text content from the response
        string? content = null;
        if (response.Content != null && response.Content.Any())
        {
            var textBlock = response.Content.FirstOrDefault(c => c.Type == "text");
            if (textBlock != null)
            {
                content = textBlock.Text;
            }
        }

        // For streaming, handle delta
        Message? delta = null;
        if (response.Delta != null)
        {
            delta = new Message
            {
                Role = "assistant",
                Content = response.Delta.Text,
                ToolCalls = response.Delta.ToolCalls?.Select(tc => new ToolCall
                {
                    Id = tc.Id,
                    Type = tc.Type,
                    Function = new FunctionCall
                    {
                        Name = tc.Function.Name,
                        Arguments = tc.Function.Arguments
                    }
                }).ToList()
            };
        }

        // Use the full response for usage if available
        var usage = fullResponse != null ? fullResponse.Usage : response.Usage;

        return new CompletionResponse
        {
            Id = response.Id,
            Object = "chat.completion",
            Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Model = response.Model,
            Provider = Name,
            Choices = new List<CompletionChoice>
            {
                new CompletionChoice
                {
                    Index = 0,
                    Message = new Message
                    {
                        Role = response.Role,
                        Content = content,
                        ToolCalls = response.ToolCalls?.Select(tc => new ToolCall
                        {
                            Id = tc.Id,
                            Type = tc.Type,
                            Function = new FunctionCall
                            {
                                Name = tc.Function.Name,
                                Arguments = tc.Function.Arguments
                            }
                        }).ToList()
                    },
                    FinishReason = response.StopReason,
                    Delta = delta
                }
            },
            Usage = new CompletionUsage
            {
                PromptTokens = usage?.InputTokens ?? 0,
                CompletionTokens = usage?.OutputTokens ?? 0,
                TotalTokens = (usage?.InputTokens ?? 0) + (usage?.OutputTokens ?? 0)
            }
        };
    }
}
