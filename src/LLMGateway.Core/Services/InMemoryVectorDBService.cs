using LLMGateway.Core.Exceptions;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Completion;
using LLMGateway.Core.Models.Embedding;
using LLMGateway.Core.Models.VectorDB;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LLMGateway.Core.Services;

/// <summary>
/// In-memory vector database service
/// </summary>
public class InMemoryVectorDBService : IVectorDBService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly ICompletionService _completionService;
    private readonly VectorDBOptions _options;
    private readonly ILogger<InMemoryVectorDBService> _logger;
    
    private readonly Dictionary<string, (int Dimensions, SimilarityMetric Metric, Dictionary<string, VectorRecord> Records)> _namespaces = new();

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="embeddingService">Embedding service</param>
    /// <param name="completionService">Completion service</param>
    /// <param name="options">Vector DB options</param>
    /// <param name="logger">Logger</param>
    public InMemoryVectorDBService(
        IEmbeddingService embeddingService,
        ICompletionService completionService,
        IOptions<VectorDBOptions> options,
        ILogger<InMemoryVectorDBService> logger)
    {
        _embeddingService = embeddingService;
        _completionService = completionService;
        _options = options.Value;
        _logger = logger;
        
        // Create default namespace
        _namespaces[_options.DefaultNamespace] = (_options.DefaultDimensions, _options.DefaultSimilarityMetric, new Dictionary<string, VectorRecord>());
    }

    /// <inheritdoc/>
    public VectorDBProviderType GetProviderType() => VectorDBProviderType.InMemory;

    /// <inheritdoc/>
    public Task CreateNamespaceAsync(string namespaceName, int dimensions, SimilarityMetric metric)
    {
        if (_namespaces.ContainsKey(namespaceName))
        {
            throw new ValidationException($"Namespace '{namespaceName}' already exists");
        }
        
        _namespaces[namespaceName] = (dimensions, metric, new Dictionary<string, VectorRecord>());
        
        _logger.LogInformation("Created namespace {Namespace} with dimensions {Dimensions} and metric {Metric}", 
            namespaceName, dimensions, metric);
            
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task DeleteNamespaceAsync(string namespaceName)
    {
        if (!_namespaces.ContainsKey(namespaceName))
        {
            throw new NotFoundException($"Namespace '{namespaceName}' not found");
        }
        
        _namespaces.Remove(namespaceName);
        
        _logger.LogInformation("Deleted namespace {Namespace}", namespaceName);
        
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<string>> ListNamespacesAsync()
    {
        return Task.FromResult<IEnumerable<string>>(_namespaces.Keys);
    }

    /// <inheritdoc/>
    public Task UpsertAsync(VectorUpsertRequest request)
    {
        var namespaceName = request.Namespace ?? _options.DefaultNamespace;
        
        if (!_namespaces.ContainsKey(namespaceName))
        {
            throw new NotFoundException($"Namespace '{namespaceName}' not found");
        }
        
        var (dimensions, _, records) = _namespaces[namespaceName];
        
        foreach (var record in request.Records)
        {
            if (record.Vector.Length != dimensions)
            {
                throw new ValidationException($"Vector dimensions mismatch. Expected {dimensions}, got {record.Vector.Length}");
            }
            
            record.Namespace = namespaceName;
            records[record.Id] = record;
        }
        
        _logger.LogInformation("Upserted {Count} vectors in namespace {Namespace}", 
            request.Records.Count, namespaceName);
            
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task DeleteAsync(VectorDeleteRequest request)
    {
        var namespaceName = request.Namespace ?? _options.DefaultNamespace;
        
        if (!_namespaces.ContainsKey(namespaceName))
        {
            throw new NotFoundException($"Namespace '{namespaceName}' not found");
        }
        
        var (_, _, records) = _namespaces[namespaceName];
        
        if (request.Ids != null && request.Ids.Count > 0)
        {
            foreach (var id in request.Ids)
            {
                records.Remove(id);
            }
            
            _logger.LogInformation("Deleted {Count} vectors by ID in namespace {Namespace}", 
                request.Ids.Count, namespaceName);
        }
        
        if (request.Filter != null && request.Filter.Count > 0)
        {
            var idsToRemove = records.Values
                .Where(r => MatchesFilter(r, request.Filter))
                .Select(r => r.Id)
                .ToList();
                
            foreach (var id in idsToRemove)
            {
                records.Remove(id);
            }
            
            _logger.LogInformation("Deleted {Count} vectors by filter in namespace {Namespace}", 
                idsToRemove.Count, namespaceName);
        }
        
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<SimilarityResult>> SearchAsync(VectorSearchRequest request)
    {
        var namespaceName = request.Namespace ?? _options.DefaultNamespace;
        
        if (!_namespaces.ContainsKey(namespaceName))
        {
            throw new NotFoundException($"Namespace '{namespaceName}' not found");
        }
        
        var (dimensions, metric, records) = _namespaces[namespaceName];
        
        if (request.QueryVector.Length != dimensions)
        {
            throw new ValidationException($"Query vector dimensions mismatch. Expected {dimensions}, got {request.QueryVector.Length}");
        }
        
        var results = records.Values
            .Where(r => request.Filter == null || MatchesFilter(r, request.Filter))
            .Select(r => new SimilarityResult
            {
                Id = r.Id,
                Text = r.Text,
                Score = CalculateSimilarity(request.QueryVector, r.Vector, metric),
                Metadata = r.Metadata,
                Vector = request.IncludeVectors ? r.Vector : null
            })
            .Where(r => r.Score >= request.MinScore)
            .OrderByDescending(r => r.Score)
            .Take(request.TopK)
            .ToList();
            
        _logger.LogInformation("Found {Count} results in namespace {Namespace}", 
            results.Count, namespaceName);
            
        return Task.FromResult<IEnumerable<SimilarityResult>>(results);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<SimilarityResult>> SearchByTextAsync(TextSearchRequest request)
    {
        // Create embedding for the query text
        var embeddingRequest = new EmbeddingRequest
        {
            ModelId = request.EmbeddingModelId,
            Input = request.QueryText
        };
        
        var embeddingResponse = await _embeddingService.CreateEmbeddingAsync(embeddingRequest);
        
        if (embeddingResponse.Data.Count == 0 || embeddingResponse.Data[0].Embedding.Count == 0)
        {
            throw new Exception("Failed to create embedding for query text");
        }
        
        // Search using the embedding
        var searchRequest = new VectorSearchRequest
        {
            QueryVector = embeddingResponse.Data[0].Embedding.ToArray(),
            Namespace = request.Namespace,
            Filter = request.Filter,
            TopK = request.TopK,
            MinScore = request.MinScore,
            IncludeVectors = request.IncludeVectors,
            IncludeMetadata = request.IncludeMetadata
        };
        
        return await SearchAsync(searchRequest);
    }

    /// <inheritdoc/>
    public async Task<CompletionResponse> PerformRAGAsync(RAGRequest request)
    {
        // Search for relevant documents
        var searchRequest = new TextSearchRequest
        {
            QueryText = request.Query,
            EmbeddingModelId = request.EmbeddingModelId,
            Namespace = request.Namespace,
            Filter = request.Filter,
            TopK = request.MaxRelevantDocs,
            MinScore = request.MinRelevanceScore,
            IncludeVectors = false,
            IncludeMetadata = true
        };
        
        var searchResults = await SearchByTextAsync(searchRequest);
        
        // Build context from search results
        var context = string.Join("\n\n", searchResults.Select(r => r.Text));
        
        // Create completion request with context
        var messages = new List<Message>
        {
            new Message { Role = "system", Content = request.SystemPrompt },
            new Message { Role = "user", Content = $"Context:\n{context}\n\nQuestion: {request.Query}" }
        };
        
        var completionRequest = new CompletionRequest
        {
            ModelId = request.CompletionModelId,
            Messages = messages,
            Temperature = request.Temperature,
            MaxTokens = request.MaxTokens,
            User = request.User
        };
        
        // Get completion
        return await _completionService.CreateCompletionAsync(completionRequest);
    }

    /// <inheritdoc/>
    public Task<VectorRecord?> GetByIdAsync(string id, string? namespaceName = null)
    {
        namespaceName ??= _options.DefaultNamespace;
        
        if (!_namespaces.ContainsKey(namespaceName))
        {
            throw new NotFoundException($"Namespace '{namespaceName}' not found");
        }
        
        var (_, _, records) = _namespaces[namespaceName];
        
        records.TryGetValue(id, out var record);
        
        return Task.FromResult(record);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<VectorRecord>> GetByIdsAsync(IEnumerable<string> ids, string? namespaceName = null)
    {
        namespaceName ??= _options.DefaultNamespace;
        
        if (!_namespaces.ContainsKey(namespaceName))
        {
            throw new NotFoundException($"Namespace '{namespaceName}' not found");
        }
        
        var (_, _, records) = _namespaces[namespaceName];
        
        var result = ids
            .Where(id => records.ContainsKey(id))
            .Select(id => records[id])
            .ToList();
            
        return Task.FromResult<IEnumerable<VectorRecord>>(result);
    }

    /// <inheritdoc/>
    public Task<long> GetCountAsync(string? namespaceName = null)
    {
        namespaceName ??= _options.DefaultNamespace;
        
        if (!_namespaces.ContainsKey(namespaceName))
        {
            throw new NotFoundException($"Namespace '{namespaceName}' not found");
        }
        
        var (_, _, records) = _namespaces[namespaceName];
        
        return Task.FromResult((long)records.Count);
    }
    
    #region Helper methods
    
    private static bool MatchesFilter(VectorRecord record, Dictionary<string, string> filter)
    {
        foreach (var (key, value) in filter)
        {
            if (!record.Metadata.TryGetValue(key, out var recordValue) || recordValue != value)
            {
                return false;
            }
        }
        
        return true;
    }
    
    private static float CalculateSimilarity(float[] v1, float[] v2, SimilarityMetric metric)
    {
        return metric switch
        {
            SimilarityMetric.Cosine => CalculateCosineSimilarity(v1, v2),
            SimilarityMetric.Euclidean => CalculateEuclideanSimilarity(v1, v2),
            SimilarityMetric.DotProduct => CalculateDotProduct(v1, v2),
            _ => throw new ArgumentOutOfRangeException(nameof(metric), metric, "Unsupported similarity metric")
        };
    }
    
    private static float CalculateCosineSimilarity(float[] v1, float[] v2)
    {
        float dotProduct = 0;
        float norm1 = 0;
        float norm2 = 0;
        
        for (int i = 0; i < v1.Length; i++)
        {
            dotProduct += v1[i] * v2[i];
            norm1 += v1[i] * v1[i];
            norm2 += v2[i] * v2[i];
        }
        
        return dotProduct / (float)(Math.Sqrt(norm1) * Math.Sqrt(norm2));
    }
    
    private static float CalculateEuclideanSimilarity(float[] v1, float[] v2)
    {
        float sum = 0;
        
        for (int i = 0; i < v1.Length; i++)
        {
            float diff = v1[i] - v2[i];
            sum += diff * diff;
        }
        
        // Convert distance to similarity (1 / (1 + distance))
        return 1 / (1 + (float)Math.Sqrt(sum));
    }
    
    private static float CalculateDotProduct(float[] v1, float[] v2)
    {
        float dotProduct = 0;
        
        for (int i = 0; i < v1.Length; i++)
        {
            dotProduct += v1[i] * v2[i];
        }
        
        return dotProduct;
    }
    
    #endregion
}
