using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LLMGateway.Tuning.Core.Interfaces
{
    public interface IPerformanceAnalyzer
    {
        Task<Dictionary<string, double>> CalculateMetricsAsync(string modelId, DateTime since);
        Task<bool> IsModelPerformingWellAsync(string modelId, Dictionary<string, double> thresholds);
    }
}
