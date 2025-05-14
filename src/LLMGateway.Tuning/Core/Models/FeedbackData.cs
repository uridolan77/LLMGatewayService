using System;
using System.Collections.Generic;
using LLMGateway.Tuning.Core.Enums;

namespace LLMGateway.Tuning.Core.Models
{
    public record FeedbackData
    {
        // Core feedback data
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public string UserId { get; init; }
        public string OriginalPrompt { get; init; }
        public string ModelResponse { get; init; }
        public Dictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
        public FeedbackType FeedbackType { get; init; }
        
        // User-provided corrections
        public string CorrectedResponse { get; init; }
        
        // Feedback score (-1 to 1)
        public double SatisfactionScore { get; init; }
        
        // Contextual information
        public string UserSegment { get; init; }
        public List<string> UserContext { get; init; } = new List<string>();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public RequestContext RequestContext { get; init; }
    }

    public record RequestContext
    {
        public string SessionId { get; init; }
        public string ClientApplication { get; init; }
        public Dictionary<string, string> RequestParameters { get; init; } = new Dictionary<string, string>();
        public int PromptTokens { get; init; }
        public int ResponseTokens { get; init; }
        public double ProcessingTimeMs { get; init; }
    }
}
