namespace LLMGateway.Tuning;

/// <summary>
/// LLMGateway.Tuning provides a comprehensive framework for collecting feedback,
/// generating training datasets, fine-tuning models, evaluating performance,
/// and deploying LLM models safely using strategies like canary deployment.
/// </summary>
/// <remarks>
/// Key components include:
/// - FeedbackCollector: For gathering and storing user feedback
/// - DatasetGenerator: For creating and managing training datasets
/// - ModelTrainingJob: For submitting and monitoring training jobs
/// - ModelEvaluator: For evaluating model performance
/// - CanaryDeployer: For safely deploying models with monitoring
/// </remarks>
public static class TuningLibraryInfo
{
    public static string Description => "A comprehensive framework for LLM model tuning, evaluation and deployment";
    public static string Version => "1.0.0";

    public static string GetLibraryInformation()
    {
        return $"LLMGateway.Tuning v{Version}: {Description}";
    }
}
