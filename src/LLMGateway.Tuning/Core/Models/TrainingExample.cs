using System;
using System.Collections.Generic;

namespace LLMGateway.Tuning.Core.Models
{
    public record TrainingExample
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public string SystemPrompt { get; init; }
        public string UserPrompt { get; init; }
        public string ModelResponse { get; init; }
        public string PreferredResponse { get; init; }
        public List<string> Tags { get; init; } = new List<string>();
        public double Difficulty { get; init; } = 0.5;
        public Dictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
    }
}
