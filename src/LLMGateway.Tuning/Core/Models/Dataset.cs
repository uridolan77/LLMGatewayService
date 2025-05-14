using System;
using System.Collections.Generic;
using LLMGateway.Tuning.Core.Enums;

namespace LLMGateway.Tuning.Core.Models
{
    public record Dataset
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public string Name { get; init; }
        public string Description { get; init; }
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
        public string CreatedBy { get; init; }
        public DatasetType Type { get; init; }
        public DatasetStatus Status { get; init; } = DatasetStatus.Created;
        public long Size { get; init; }
        public int ExampleCount { get; init; }
        public Dictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
        public List<DatasetSplit> Splits { get; init; } = new List<DatasetSplit>();
    }

    public record DatasetSplit
    {
        public string Name { get; init; }
        public string StoragePath { get; init; }
        public int ExampleCount { get; init; }
        public long Size { get; init; }
    }
}
