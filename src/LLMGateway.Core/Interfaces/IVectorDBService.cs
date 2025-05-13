using LLMGateway.Core.Models.Completion;
using LLMGateway.Core.Models.VectorDB;

namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Interface for vector database service
/// </summary>
public interface IVectorDBService
{
    /// <summary>
    /// Get provider type
    /// </summary>
    /// <returns>Provider type</returns>
    VectorDBProviderType GetProviderType();
    
    /// <summary>
    /// Create namespace/collection
    /// </summary>
    /// <param name="namespaceName">Namespace name</param>
    /// <param name="dimensions">Dimensions</param>
    /// <param name="metric">Similarity metric</param>
    /// <returns>Task</returns>
    Task CreateNamespaceAsync(string namespaceName, int dimensions, SimilarityMetric metric);
    
    /// <summary>
    /// Delete namespace/collection
    /// </summary>
    /// <param name="namespaceName">Namespace name</param>
    /// <returns>Task</returns>
    Task DeleteNamespaceAsync(string namespaceName);
    
    /// <summary>
    /// List namespaces/collections
    /// </summary>
    /// <returns>List of namespaces</returns>
    Task<IEnumerable<string>> ListNamespacesAsync();
    
    /// <summary>
    /// Upsert vectors
    /// </summary>
    /// <param name="request">Upsert request</param>
    /// <returns>Task</returns>
    Task UpsertAsync(VectorUpsertRequest request);
    
    /// <summary>
    /// Delete vectors
    /// </summary>
    /// <param name="request">Delete request</param>
    /// <returns>Task</returns>
    Task DeleteAsync(VectorDeleteRequest request);
    
    /// <summary>
    /// Search vectors
    /// </summary>
    /// <param name="request">Search request</param>
    /// <returns>Search results</returns>
    Task<IEnumerable<SimilarityResult>> SearchAsync(VectorSearchRequest request);
    
    /// <summary>
    /// Search by text
    /// </summary>
    /// <param name="request">Text search request</param>
    /// <returns>Search results</returns>
    Task<IEnumerable<SimilarityResult>> SearchByTextAsync(TextSearchRequest request);
    
    /// <summary>
    /// Perform RAG (Retrieval-Augmented Generation)
    /// </summary>
    /// <param name="request">RAG request</param>
    /// <returns>Completion response</returns>
    Task<CompletionResponse> PerformRAGAsync(RAGRequest request);
    
    /// <summary>
    /// Get vector by ID
    /// </summary>
    /// <param name="id">Record ID</param>
    /// <param name="namespaceName">Namespace name</param>
    /// <returns>Vector record</returns>
    Task<VectorRecord?> GetByIdAsync(string id, string? namespaceName = null);
    
    /// <summary>
    /// Get vectors by IDs
    /// </summary>
    /// <param name="ids">Record IDs</param>
    /// <param name="namespaceName">Namespace name</param>
    /// <returns>Vector records</returns>
    Task<IEnumerable<VectorRecord>> GetByIdsAsync(IEnumerable<string> ids, string? namespaceName = null);
    
    /// <summary>
    /// Get vector count
    /// </summary>
    /// <param name="namespaceName">Namespace name</param>
    /// <returns>Vector count</returns>
    Task<long> GetCountAsync(string? namespaceName = null);
}
