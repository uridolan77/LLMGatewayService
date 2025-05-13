using LLMGateway.API.Controllers.Base;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Completion;
using LLMGateway.Core.Models.VectorDB;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LLMGateway.API.Controllers;

/// <summary>
/// Controller for vector database operations
/// </summary>
[ApiVersion("1.0")]
[Authorize]
public class VectorDBController : BaseApiController
{
    private readonly IVectorDBService _vectorDBService;
    private readonly ILogger<VectorDBController> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="vectorDBService">Vector DB service</param>
    /// <param name="logger">Logger</param>
    public VectorDBController(
        IVectorDBService vectorDBService,
        ILogger<VectorDBController> logger)
    {
        _vectorDBService = vectorDBService;
        _logger = logger;
    }

    /// <summary>
    /// Get the current user ID from the claims
    /// </summary>
    /// <returns>User ID</returns>
    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
    }

    /// <summary>
    /// Get provider type
    /// </summary>
    /// <returns>Provider type</returns>
    [HttpGet("provider")]
    [ProducesResponseType(typeof(VectorDBProviderType), StatusCodes.Status200OK)]
    public ActionResult<VectorDBProviderType> GetProviderType()
    {
        var providerType = _vectorDBService.GetProviderType();
        return Ok(providerType);
    }

    /// <summary>
    /// Create namespace/collection
    /// </summary>
    /// <param name="namespaceName">Namespace name</param>
    /// <param name="dimensions">Dimensions</param>
    /// <param name="metric">Similarity metric</param>
    /// <returns>No content</returns>
    [HttpPost("namespaces/{namespaceName}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> CreateNamespaceAsync(
        string namespaceName,
        [FromQuery] int dimensions = 1536,
        [FromQuery] SimilarityMetric metric = SimilarityMetric.Cosine)
    {
        await _vectorDBService.CreateNamespaceAsync(namespaceName, dimensions, metric);
        return NoContent();
    }

    /// <summary>
    /// Delete namespace/collection
    /// </summary>
    /// <param name="namespaceName">Namespace name</param>
    /// <returns>No content</returns>
    [HttpDelete("namespaces/{namespaceName}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteNamespaceAsync(string namespaceName)
    {
        await _vectorDBService.DeleteNamespaceAsync(namespaceName);
        return NoContent();
    }

    /// <summary>
    /// List namespaces/collections
    /// </summary>
    /// <returns>List of namespaces</returns>
    [HttpGet("namespaces")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<string>>> ListNamespacesAsync()
    {
        var namespaces = await _vectorDBService.ListNamespacesAsync();
        return Ok(namespaces);
    }

    /// <summary>
    /// Upsert vectors
    /// </summary>
    /// <param name="request">Upsert request</param>
    /// <returns>No content</returns>
    [HttpPost("vectors")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpsertAsync(VectorUpsertRequest request)
    {
        await _vectorDBService.UpsertAsync(request);
        return NoContent();
    }

    /// <summary>
    /// Delete vectors
    /// </summary>
    /// <param name="request">Delete request</param>
    /// <returns>No content</returns>
    [HttpDelete("vectors")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteAsync(VectorDeleteRequest request)
    {
        await _vectorDBService.DeleteAsync(request);
        return NoContent();
    }

    /// <summary>
    /// Search vectors
    /// </summary>
    /// <param name="request">Search request</param>
    /// <returns>Search results</returns>
    [HttpPost("search")]
    [ProducesResponseType(typeof(IEnumerable<SimilarityResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<SimilarityResult>>> SearchAsync(VectorSearchRequest request)
    {
        var results = await _vectorDBService.SearchAsync(request);
        return Ok(results);
    }

    /// <summary>
    /// Search by text
    /// </summary>
    /// <param name="request">Text search request</param>
    /// <returns>Search results</returns>
    [HttpPost("search/text")]
    [ProducesResponseType(typeof(IEnumerable<SimilarityResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<SimilarityResult>>> SearchByTextAsync(TextSearchRequest request)
    {
        var results = await _vectorDBService.SearchByTextAsync(request);
        return Ok(results);
    }

    /// <summary>
    /// Perform RAG (Retrieval-Augmented Generation)
    /// </summary>
    /// <param name="request">RAG request</param>
    /// <returns>Completion response</returns>
    [HttpPost("rag")]
    [ProducesResponseType(typeof(CompletionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompletionResponse>> PerformRAGAsync(RAGRequest request)
    {
        // Set user ID if not provided
        if (string.IsNullOrEmpty(request.User))
        {
            request.User = GetUserId();
        }

        var response = await _vectorDBService.PerformRAGAsync(request);
        return Ok(response);
    }

    /// <summary>
    /// Get vector by ID
    /// </summary>
    /// <param name="id">Record ID</param>
    /// <param name="namespaceName">Namespace name</param>
    /// <returns>Vector record</returns>
    [HttpGet("vectors/{id}")]
    [ProducesResponseType(typeof(VectorRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VectorRecord>> GetByIdAsync(
        string id,
        [FromQuery] string? namespaceName = null)
    {
        var record = await _vectorDBService.GetByIdAsync(id, namespaceName);

        if (record == null)
        {
            return NotFound();
        }

        return Ok(record);
    }

    /// <summary>
    /// Get vectors by IDs
    /// </summary>
    /// <param name="ids">Record IDs</param>
    /// <param name="namespaceName">Namespace name</param>
    /// <returns>Vector records</returns>
    [HttpPost("vectors/batch")]
    [ProducesResponseType(typeof(IEnumerable<VectorRecord>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<VectorRecord>>> GetByIdsAsync(
        [FromBody] List<string> ids,
        [FromQuery] string? namespaceName = null)
    {
        var records = await _vectorDBService.GetByIdsAsync(ids, namespaceName);
        return Ok(records);
    }

    /// <summary>
    /// Get vector count
    /// </summary>
    /// <param name="namespaceName">Namespace name</param>
    /// <returns>Vector count</returns>
    [HttpGet("count")]
    [ProducesResponseType(typeof(long), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<long>> GetCountAsync([FromQuery] string? namespaceName = null)
    {
        var count = await _vectorDBService.GetCountAsync(namespaceName);
        return Ok(count);
    }
}
