namespace LLMGateway.Core.Models.VectorDB;

/// <summary>
/// Vector database provider type
/// </summary>
public enum VectorDBProviderType
{
    /// <summary>
    /// In-memory vector database
    /// </summary>
    InMemory,
    
    /// <summary>
    /// Pinecone vector database
    /// </summary>
    Pinecone,
    
    /// <summary>
    /// Milvus vector database
    /// </summary>
    Milvus,
    
    /// <summary>
    /// Qdrant vector database
    /// </summary>
    Qdrant,
    
    /// <summary>
    /// Weaviate vector database
    /// </summary>
    Weaviate,
    
    /// <summary>
    /// Redis vector database
    /// </summary>
    Redis,
    
    /// <summary>
    /// PostgreSQL with pgvector extension
    /// </summary>
    PostgreSQL
}

/// <summary>
/// Vector database options
/// </summary>
public class VectorDBOptions
{
    /// <summary>
    /// Provider type
    /// </summary>
    public VectorDBProviderType ProviderType { get; set; } = VectorDBProviderType.InMemory;
    
    /// <summary>
    /// Connection string
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
    
    /// <summary>
    /// API key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Environment (e.g., production, staging)
    /// </summary>
    public string Environment { get; set; } = "production";
    
    /// <summary>
    /// Default namespace/collection
    /// </summary>
    public string DefaultNamespace { get; set; } = "default";
    
    /// <summary>
    /// Default dimensions for vectors
    /// </summary>
    public int DefaultDimensions { get; set; } = 1536;
    
    /// <summary>
    /// Default similarity metric
    /// </summary>
    public SimilarityMetric DefaultSimilarityMetric { get; set; } = SimilarityMetric.Cosine;
}

/// <summary>
/// Similarity metric
/// </summary>
public enum SimilarityMetric
{
    /// <summary>
    /// Cosine similarity
    /// </summary>
    Cosine,
    
    /// <summary>
    /// Euclidean distance
    /// </summary>
    Euclidean,
    
    /// <summary>
    /// Dot product
    /// </summary>
    DotProduct
}

/// <summary>
/// Vector record
/// </summary>
public class VectorRecord
{
    /// <summary>
    /// Record ID
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Vector
    /// </summary>
    public float[] Vector { get; set; } = Array.Empty<float>();
    
    /// <summary>
    /// Text
    /// </summary>
    public string Text { get; set; } = string.Empty;
    
    /// <summary>
    /// Metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
    
    /// <summary>
    /// Namespace/collection
    /// </summary>
    public string Namespace { get; set; } = string.Empty;
}

/// <summary>
/// Similarity search result
/// </summary>
public class SimilarityResult
{
    /// <summary>
    /// Record ID
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Text
    /// </summary>
    public string Text { get; set; } = string.Empty;
    
    /// <summary>
    /// Similarity score
    /// </summary>
    public float Score { get; set; }
    
    /// <summary>
    /// Metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
    
    /// <summary>
    /// Vector
    /// </summary>
    public float[]? Vector { get; set; }
}

/// <summary>
/// Vector search request
/// </summary>
public class VectorSearchRequest
{
    /// <summary>
    /// Query vector
    /// </summary>
    public float[] QueryVector { get; set; } = Array.Empty<float>();
    
    /// <summary>
    /// Namespace/collection
    /// </summary>
    public string? Namespace { get; set; }
    
    /// <summary>
    /// Filter
    /// </summary>
    public Dictionary<string, string>? Filter { get; set; }
    
    /// <summary>
    /// Top K results
    /// </summary>
    public int TopK { get; set; } = 10;
    
    /// <summary>
    /// Minimum score threshold
    /// </summary>
    public float MinScore { get; set; } = 0.0f;
    
    /// <summary>
    /// Include vectors in results
    /// </summary>
    public bool IncludeVectors { get; set; } = false;
    
    /// <summary>
    /// Include metadata in results
    /// </summary>
    public bool IncludeMetadata { get; set; } = true;
}

/// <summary>
/// Vector upsert request
/// </summary>
public class VectorUpsertRequest
{
    /// <summary>
    /// Records to upsert
    /// </summary>
    public List<VectorRecord> Records { get; set; } = new();
    
    /// <summary>
    /// Namespace/collection
    /// </summary>
    public string? Namespace { get; set; }
}

/// <summary>
/// Vector delete request
/// </summary>
public class VectorDeleteRequest
{
    /// <summary>
    /// IDs to delete
    /// </summary>
    public List<string> Ids { get; set; } = new();
    
    /// <summary>
    /// Namespace/collection
    /// </summary>
    public string? Namespace { get; set; }
    
    /// <summary>
    /// Filter
    /// </summary>
    public Dictionary<string, string>? Filter { get; set; }
}

/// <summary>
/// Vector search by text request
/// </summary>
public class TextSearchRequest
{
    /// <summary>
    /// Query text
    /// </summary>
    public string QueryText { get; set; } = string.Empty;
    
    /// <summary>
    /// Embedding model ID
    /// </summary>
    public string EmbeddingModelId { get; set; } = string.Empty;
    
    /// <summary>
    /// Namespace/collection
    /// </summary>
    public string? Namespace { get; set; }
    
    /// <summary>
    /// Filter
    /// </summary>
    public Dictionary<string, string>? Filter { get; set; }
    
    /// <summary>
    /// Top K results
    /// </summary>
    public int TopK { get; set; } = 10;
    
    /// <summary>
    /// Minimum score threshold
    /// </summary>
    public float MinScore { get; set; } = 0.0f;
    
    /// <summary>
    /// Include vectors in results
    /// </summary>
    public bool IncludeVectors { get; set; } = false;
    
    /// <summary>
    /// Include metadata in results
    /// </summary>
    public bool IncludeMetadata { get; set; } = true;
}

/// <summary>
/// RAG (Retrieval-Augmented Generation) request
/// </summary>
public class RAGRequest
{
    /// <summary>
    /// Query text
    /// </summary>
    public string Query { get; set; } = string.Empty;
    
    /// <summary>
    /// Embedding model ID
    /// </summary>
    public string EmbeddingModelId { get; set; } = string.Empty;
    
    /// <summary>
    /// Completion model ID
    /// </summary>
    public string CompletionModelId { get; set; } = string.Empty;
    
    /// <summary>
    /// Namespace/collection
    /// </summary>
    public string? Namespace { get; set; }
    
    /// <summary>
    /// Filter
    /// </summary>
    public Dictionary<string, string>? Filter { get; set; }
    
    /// <summary>
    /// Number of relevant documents to retrieve
    /// </summary>
    public int MaxRelevantDocs { get; set; } = 5;
    
    /// <summary>
    /// Minimum relevance score
    /// </summary>
    public float MinRelevanceScore { get; set; } = 0.7f;
    
    /// <summary>
    /// System prompt
    /// </summary>
    public string SystemPrompt { get; set; } = "You are a helpful assistant that answers questions based on the provided context.";
    
    /// <summary>
    /// Temperature
    /// </summary>
    public float? Temperature { get; set; }
    
    /// <summary>
    /// Max tokens
    /// </summary>
    public int? MaxTokens { get; set; }
    
    /// <summary>
    /// User ID
    /// </summary>
    public string? User { get; set; }
}
