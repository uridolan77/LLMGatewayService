using System;
using System.Collections.Generic;
using LLMGateway.Tuning.Core.Enums;

namespace LLMGateway.Tuning.Training.Configuration
{
    public class TrainingConfiguration
    {
        public ModelType ModelType { get; set; }
        public string BaseModelId { get; set; }
        public int Epochs { get; set; } = 3;
        public int BatchSize { get; set; } = 4;
        public double LearningRate { get; set; } = 0.0001;
        public bool EarlyStoppingEnabled { get; set; } = true;
        public int EarlyStoppingPatience { get; set; } = 3;
        public ValidationStrategy ValidationStrategy { get; set; } = ValidationStrategy.HoldOut;
        public double ValidationSplit { get; set; } = 0.1;
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, object> ModelSpecificParams { get; set; } = new Dictionary<string, object>();
    }

    public enum ValidationStrategy
    {
        HoldOut,
        KFold,
        ProgressiveValidation
    }
}
