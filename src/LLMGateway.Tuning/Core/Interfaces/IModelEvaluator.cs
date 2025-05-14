using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LLMGateway.Tuning.Core.Interfaces
{
    public interface IModelEvaluator
    {
        Task<EvaluationResult> EvaluateModelAsync(ModelEvaluationRequest request);
    }

    public class ModelEvaluationRequest
    {
        public string ModelId { get; set; }
        public string DatasetId { get; set; }
        public string DatasetSplit { get; set; } = "validation";
        public List<string> Metrics { get; set; } = new List<string> { "exactMatch", "bleu", "rouge" };
        public bool IncludeDetailedResults { get; set; } = false;
    }

    public class EvaluationResult
    {
        public string ModelId { get; set; }
        public string DatasetId { get; set; }
        public DateTime EvaluationTimestamp { get; set; }
        public int ExamplesCount { get; set; }
        public Dictionary<string, double> Metrics { get; set; } = new Dictionary<string, double>();
        public List<PredictionResult> DetailedResults { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class PredictionResult
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Prediction { get; set; }
        public string Target { get; set; }
        public Dictionary<string, double> Scores { get; set; } = new Dictionary<string, double>();
    }
}
