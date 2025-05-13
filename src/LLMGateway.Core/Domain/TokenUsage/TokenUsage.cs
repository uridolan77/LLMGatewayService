using System;
using System.Collections.Generic;

namespace LLMGateway.Core.Domain.TokenUsage;

/// <summary>
/// Domain entity for token usage
/// </summary>
public class TokenUsage
{
    /// <summary>
    /// ID of the token usage record
    /// </summary>
    public string Id { get; private set; }
    
    /// <summary>
    /// Timestamp of when the usage occurred
    /// </summary>
    public DateTimeOffset Timestamp { get; private set; }
    
    /// <summary>
    /// User ID associated with the token usage
    /// </summary>
    public string UserId { get; private set; }
    
    /// <summary>
    /// API key ID associated with the token usage
    /// </summary>
    public string ApiKeyId { get; private set; }
    
    /// <summary>
    /// Request ID associated with the token usage
    /// </summary>
    public string RequestId { get; private set; }
    
    /// <summary>
    /// Model ID used for the request
    /// </summary>
    public string ModelId { get; private set; }
    
    /// <summary>
    /// Provider name
    /// </summary>
    public string Provider { get; private set; }
    
    /// <summary>
    /// Type of request (completion, embedding, etc.)
    /// </summary>
    public string RequestType { get; private set; }
    
    /// <summary>
    /// Number of prompt tokens
    /// </summary>
    public int PromptTokens { get; private set; }
    
    /// <summary>
    /// Number of completion tokens
    /// </summary>
    public int CompletionTokens { get; private set; }
    
    /// <summary>
    /// Total number of tokens
    /// </summary>
    public int TotalTokens => PromptTokens + CompletionTokens;
    
    /// <summary>
    /// Cost of the request in USD
    /// </summary>
    public decimal Cost { get; private set; }
    
    /// <summary>
    /// Private constructor for EF Core
    /// </summary>
    private TokenUsage() 
    {
        Id = Guid.NewGuid().ToString();
        Timestamp = DateTimeOffset.UtcNow;
    }
    
    /// <summary>
    /// Creates a new token usage record
    /// </summary>
    public static TokenUsage Create(
        string userId,
        string apiKeyId,
        string requestId,
        string modelId,
        string provider,
        string requestType,
        int promptTokens,
        int completionTokens,
        decimal cost)
    {
        var tokenUsage = new TokenUsage
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTimeOffset.UtcNow,
            UserId = userId,
            ApiKeyId = apiKeyId,
            RequestId = requestId,
            ModelId = modelId,
            Provider = provider,
            RequestType = requestType,
            PromptTokens = promptTokens,
            CompletionTokens = completionTokens,
            Cost = cost
        };
        
        return tokenUsage;
    }
    
    /// <summary>
    /// Updates the cost of the token usage
    /// </summary>
    public void UpdateCost(decimal newCost)
    {
        Cost = newCost;
    }
}
