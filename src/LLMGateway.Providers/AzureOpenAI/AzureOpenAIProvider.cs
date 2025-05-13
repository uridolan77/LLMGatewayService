using LLMGateway.Core.Exceptions;
using LLMGateway.Core.Models.Completion;
using LLMGateway.Core.Models.Embedding;
using LLMGateway.Core.Models.Provider;
using LLMGateway.Providers.AzureOpenAI.Models;
using LLMGateway.Providers.Base;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LLMGateway.Providers.AzureOpenAI;

/// <summary>
/// Azure OpenAI provider
/// </summary>
public class AzureOpenAIProvider : BaseLLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AzureOpenAIOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="httpClient">HTTP client</param>
    /// <param name="httpClientFactory">HTTP client factory</param>
    /// <param name="options">Azure OpenAI options</param>
    /// <param name="logger">Logger</param>
    public AzureOpenAIProvider(
        HttpClient httpClient,
        IHttpClientFactory httpClientFactory,
        IOptions<AzureOpenAIOptions> options,
        ILogger<AzureOpenAIProvider> logger)
        : base(logger)
    {
        _httpClient = httpClient;
        _httpClientFactory = httpClientFactory;
        _options = options.Value;

        // Configure the HTTP client
        _httpClient.DefaultRequestHeaders.Add("api-key", _options.ApiKey);

        // Configure JSON options
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
    }

    /// <inheritdoc/>
    public override string Name => "AzureOpenAI";

    /// <inheritdoc/>
    public override async Task<IEnumerable<ModelInfo>> GetModelsAsync()
    {
        try
        {
            var models = new List<ModelInfo>();

            // Azure OpenAI doesn't have a list models endpoint that returns all deployments
            // Instead, we'll return a list of models based on the configured deployments
            foreach (var deployment in _options.Deployments)
            {
                models.Add(new ModelInfo
                {
                    Id = $"azure-openai.{deployment.DeploymentId}",
                    DisplayName = deployment.DisplayName ?? deployment.DeploymentId,
                    Provider = Name,
                    ProviderModelId = deployment.DeploymentId,
                    ContextWindow = GetContextWindowForModel(deployment.ModelName),
                    SupportsCompletions = deployment.Type == AzureOpenAIDeploymentType.Completion,
                    SupportsEmbeddings = deployment.Type == AzureOpenAIDeploymentType.Embedding,
                    SupportsStreaming = deployment.Type == AzureOpenAIDeploymentType.Completion,
                    SupportsFunctionCalling = deployment.ModelName.Contains("gpt-4") || deployment.ModelName.Contains("gpt-3.5"),
                    SupportsVision = deployment.ModelName.Contains("vision")
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
            // Extract the deployment ID from the model ID
            var deploymentId = ExtractDeploymentId(modelId);

            // Find the deployment in the configuration
            var deployment = _options.Deployments.FirstOrDefault(d => d.DeploymentId == deploymentId);
            if (deployment == null)
            {
                throw new ModelNotFoundException(modelId);
            }

            return new ModelInfo
            {
                Id = $"azure-openai.{deployment.DeploymentId}",
                DisplayName = deployment.DisplayName ?? deployment.DeploymentId,
                Provider = Name,
                ProviderModelId = deployment.DeploymentId,
                ContextWindow = GetContextWindowForModel(deployment.ModelName),
                SupportsCompletions = deployment.Type == AzureOpenAIDeploymentType.Completion,
                SupportsEmbeddings = deployment.Type == AzureOpenAIDeploymentType.Embedding,
                SupportsStreaming = deployment.Type == AzureOpenAIDeploymentType.Completion,
                SupportsFunctionCalling = deployment.ModelName.Contains("gpt-4") || deployment.ModelName.Contains("gpt-3.5"),
                SupportsVision = deployment.ModelName.Contains("vision")
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
            // Extract the deployment ID from the model ID
            var deploymentId = ExtractDeploymentId(request.ModelId);

            // Find the deployment in the configuration
            var deployment = _options.Deployments.FirstOrDefault(d => d.DeploymentId == deploymentId);
            if (deployment == null)
            {
                throw new ModelNotFoundException(request.ModelId);
            }

            // Ensure the deployment supports completions
            if (deployment.Type != AzureOpenAIDeploymentType.Completion)
            {
                throw new ProviderException(Name, $"Deployment {deploymentId} does not support completions");
            }

            // Build the request URL
            var requestUrl = BuildCompletionUrl(deployment);

            // Convert the request to Azure OpenAI format
            var azureRequest = ConvertToAzureOpenAICompletionRequest(request);

            // Send the request
            var response = await _httpClient.PostAsJsonAsync(requestUrl, azureRequest, _jsonOptions, cancellationToken).ConfigureAwait(false);

            // Check for errors
            response.EnsureSuccessStatusCode();

            // Parse the response
            var azureResponse = await response.Content.ReadFromJsonAsync<AzureOpenAIChatCompletionResponse>(_jsonOptions, cancellationToken).ConfigureAwait(false);

            if (azureResponse == null)
            {
                throw new ProviderException(Name, "Failed to create completion: Empty response");
            }

            // Convert the response to the standard format
            return ConvertFromAzureOpenAICompletionResponse(azureResponse);
        }
        catch (Exception ex)
        {
            throw HandleProviderException(ex, "Failed to create completion");
        }
    }

    /// <inheritdoc/>
    public override IAsyncEnumerable<CompletionResponse> CreateCompletionStreamAsync(
        CompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        return ProcessCompletionStreamAsync(request, cancellationToken);
    }

    private async IAsyncEnumerable<CompletionResponse> ProcessCompletionStreamAsync(
        CompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Extract the deployment ID from the model ID
        string deploymentId;
        HttpResponseMessage response;
        StreamReader reader;

        try
        {
            deploymentId = ExtractDeploymentId(request.ModelId);

            // Find the deployment in the configuration
            var deployment = _options.Deployments.FirstOrDefault(d => d.DeploymentId == deploymentId);
            if (deployment == null)
            {
                throw new ModelNotFoundException(request.ModelId);
            }

            // Ensure the deployment supports completions
            if (deployment.Type != AzureOpenAIDeploymentType.Completion)
            {
                throw new ProviderException(Name, $"Deployment {deploymentId} does not support completions");
            }

            // Build the request URL
            var requestUrl = BuildCompletionUrl(deployment);

            // Convert the request to Azure OpenAI format
            var azureRequest = ConvertToAzureOpenAICompletionRequest(request);

            // Ensure streaming is enabled
            azureRequest.Stream = true;

            // Create the HTTP request
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(azureRequest, _jsonOptions),
                    Encoding.UTF8,
                    "application/json")
            };

            // Add the API key header
            httpRequest.Headers.Add("api-key", _options.ApiKey);

            // Send the request
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(_options.StreamTimeoutSeconds);

            response = await httpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);

            // Check for errors
            response.EnsureSuccessStatusCode();

            // Read the response stream
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            reader = new StreamReader(stream);
        }
        catch (Exception ex)
        {
            throw HandleProviderException(ex, "Failed to create streaming completion");
        }

        // Process the stream outside the try-catch block
        using (reader)
        {
            string? line;
            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                // Skip empty lines and data: [DONE] lines
                if (string.IsNullOrWhiteSpace(line) || line == "data: [DONE]")
                {
                    continue;
                }

                // Remove the "data: " prefix
                if (line.StartsWith("data: "))
                {
                    line = line[6..];
                }

                // Parse the JSON
                AzureOpenAIChatCompletionResponse? azureResponse = null;
                try
                {
                    azureResponse = JsonSerializer.Deserialize<AzureOpenAIChatCompletionResponse>(line, _jsonOptions);
                }
                catch (JsonException)
                {
                    // Skip invalid JSON
                    continue;
                }

                if (azureResponse != null)
                {
                    // Convert the response to the standard format
                    yield return ConvertFromAzureOpenAICompletionResponse(azureResponse);
                }
            }
        }
    }

    /// <inheritdoc/>
    public override async Task<EmbeddingResponse> CreateEmbeddingAsync(EmbeddingRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Extract the deployment ID from the model ID
            var deploymentId = ExtractDeploymentId(request.ModelId);

            // Find the deployment in the configuration
            var deployment = _options.Deployments.FirstOrDefault(d => d.DeploymentId == deploymentId);
            if (deployment == null)
            {
                throw new ModelNotFoundException(request.ModelId);
            }

            // Ensure the deployment supports embeddings
            if (deployment.Type != AzureOpenAIDeploymentType.Embedding)
            {
                throw new ProviderException(Name, $"Deployment {deploymentId} does not support embeddings");
            }

            // Build the request URL
            var requestUrl = BuildEmbeddingUrl(deployment);

            // Convert the request to Azure OpenAI format
            var azureRequest = ConvertToAzureOpenAIEmbeddingRequest(request);

            // Send the request
            var response = await _httpClient.PostAsJsonAsync(requestUrl, azureRequest, _jsonOptions, cancellationToken).ConfigureAwait(false);

            // Check for errors
            response.EnsureSuccessStatusCode();

            // Parse the response
            var azureResponse = await response.Content.ReadFromJsonAsync<AzureOpenAIEmbeddingResponse>(_jsonOptions, cancellationToken).ConfigureAwait(false);

            if (azureResponse == null)
            {
                throw new ProviderException(Name, "Failed to create embedding: Empty response");
            }

            // Convert the response to the standard format
            return ConvertFromAzureOpenAIEmbeddingResponse(azureResponse);
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
            // Check if at least one deployment is available
            if (_options.Deployments.Count == 0)
            {
                return false;
            }

            // Try to get a model
            var deployment = _options.Deployments.First();
            var requestUrl = BuildCompletionUrl(deployment);

            // Make a HEAD request to check if the API is available
            var request = new HttpRequestMessage(HttpMethod.Head, requestUrl);
            request.Headers.Add("api-key", _options.ApiKey);

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    #region Helper methods

    private string ExtractDeploymentId(string modelId)
    {
        // Model ID format: azure-openai.{deploymentId}
        if (modelId.StartsWith("azure-openai."))
        {
            return modelId.Substring("azure-openai.".Length);
        }

        // If the model ID doesn't have the expected format, assume it's the deployment ID
        return modelId;
    }

    private string BuildCompletionUrl(AzureOpenAIDeployment deployment)
    {
        return $"{_options.Endpoint}/openai/deployments/{deployment.DeploymentId}/chat/completions?api-version={_options.ApiVersion}";
    }

    private string BuildEmbeddingUrl(AzureOpenAIDeployment deployment)
    {
        return $"{_options.Endpoint}/openai/deployments/{deployment.DeploymentId}/embeddings?api-version={_options.ApiVersion}";
    }

    private AzureOpenAIChatCompletionRequest ConvertToAzureOpenAICompletionRequest(CompletionRequest request)
    {
        return new AzureOpenAIChatCompletionRequest
        {
            Messages = request.Messages.Select(m => new AzureOpenAIChatMessage
            {
                Role = m.Role,
                Content = m.Content,
                Name = m.Name,
                FunctionCall = m.FunctionCall != null ? new AzureOpenAIFunctionCall
                {
                    Name = m.FunctionCall.Name,
                    Arguments = m.FunctionCall.Arguments
                } : null
            }).ToList(),
            MaxTokens = request.MaxTokens,
            Temperature = request.Temperature.HasValue ? (float)request.Temperature.Value : null,
            TopP = request.TopP.HasValue ? (float)request.TopP.Value : null,
            FrequencyPenalty = request.FrequencyPenalty.HasValue ? (float)request.FrequencyPenalty.Value : null,
            PresencePenalty = request.PresencePenalty.HasValue ? (float)request.PresencePenalty.Value : null,
            Stop = request.Stop,
            Stream = request.Stream,
            Functions = request.Tools?.Where(t => t.Type == "function").Select(t => new AzureOpenAIFunction
            {
                Name = t.Function.Name,
                Description = t.Function.Description,
                Parameters = t.Function.Parameters
            }).ToList(),
            FunctionCall = request.ToolChoice?.Type == "function" ? request.ToolChoice.Function?.Name ?? "auto" : null
        };
    }

    private CompletionResponse ConvertFromAzureOpenAICompletionResponse(AzureOpenAIChatCompletionResponse response)
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
                Message = c.Message != null ? new Core.Models.Completion.Message
                {
                    Role = c.Message.Role,
                    Content = c.Message.Content,
                    Name = c.Message.Name,
                    FunctionCall = c.Message.FunctionCall != null ? new Core.Models.Completion.FunctionCall
                    {
                        Name = c.Message.FunctionCall.Name,
                        Arguments = c.Message.FunctionCall.Arguments
                    } : null
                } : new Core.Models.Completion.Message(),
                Delta = c.Delta != null ? new Core.Models.Completion.Message
                {
                    Role = c.Delta.Role,
                    Content = c.Delta.Content,
                    FunctionCall = c.Delta.FunctionCall != null ? new Core.Models.Completion.FunctionCall
                    {
                        Name = c.Delta.FunctionCall.Name,
                        Arguments = c.Delta.FunctionCall.Arguments
                    } : null
                } : null,
                FinishReason = c.FinishReason
            }).ToList(),
            Usage = response.Usage != null ? new CompletionUsage
            {
                PromptTokens = response.Usage.PromptTokens,
                CompletionTokens = response.Usage.CompletionTokens,
                TotalTokens = response.Usage.TotalTokens
            } : null
        };
    }

    private AzureOpenAIEmbeddingRequest ConvertToAzureOpenAIEmbeddingRequest(EmbeddingRequest request)
    {
        return new AzureOpenAIEmbeddingRequest
        {
            Input = request.Input,
            Dimensions = request.Dimensions,
            User = request.User
        };
    }

    private EmbeddingResponse ConvertFromAzureOpenAIEmbeddingResponse(AzureOpenAIEmbeddingResponse response)
    {
        return new EmbeddingResponse
        {
            Object = response.Object,
            Model = response.Model,
            Provider = Name,
            Data = response.Data.Select(d => new EmbeddingData
            {
                Object = d.Object,
                Embedding = d.Embedding,
                Index = d.Index
            }).ToList(),
            Usage = new EmbeddingUsage
            {
                PromptTokens = response.Usage.PromptTokens,
                TotalTokens = response.Usage.TotalTokens
            }
        };
    }

    private static int GetContextWindowForModel(string modelName)
    {
        return modelName.ToLowerInvariant() switch
        {
            var m when m.Contains("gpt-4-32k") => 32768,
            var m when m.Contains("gpt-4-turbo") => 128000,
            var m when m.Contains("gpt-4") => 8192,
            var m when m.Contains("gpt-3.5-turbo-16k") => 16384,
            var m when m.Contains("gpt-3.5-turbo") => 4096,
            var m when m.Contains("text-embedding-ada-002") => 8191,
            _ => 4096 // Default
        };
    }

    #endregion
}
