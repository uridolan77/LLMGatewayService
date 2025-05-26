using LLMGateway.Core.Models.SDK;

namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Service for managing SDK generation and distribution
/// </summary>
public interface ISDKManagementService
{
    /// <summary>
    /// Generate SDK for a specific language
    /// </summary>
    /// <param name="request">SDK generation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated SDK information</returns>
    Task<GeneratedSDK> GenerateSDKAsync(GenerateSDKRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get available SDK languages and versions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Available SDK options</returns>
    Task<IEnumerable<SDKLanguageOption>> GetAvailableSDKLanguagesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get SDK documentation
    /// </summary>
    /// <param name="language">Programming language</param>
    /// <param name="version">SDK version</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SDK documentation</returns>
    Task<SDKDocumentation> GetSDKDocumentationAsync(string language, string version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get SDK code examples
    /// </summary>
    /// <param name="language">Programming language</param>
    /// <param name="useCase">Use case category</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Code examples</returns>
    Task<IEnumerable<CodeExample>> GetCodeExamplesAsync(string language, string useCase, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate API client for user's specific configuration
    /// </summary>
    /// <param name="request">Custom client request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Custom API client</returns>
    Task<CustomAPIClient> GenerateCustomClientAsync(GenerateCustomClientRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get SDK usage analytics
    /// </summary>
    /// <param name="request">SDK analytics request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SDK usage analytics</returns>
    Task<SDKUsageAnalytics> GetSDKUsageAnalyticsAsync(SDKAnalyticsRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate SDK configuration
    /// </summary>
    /// <param name="request">Validation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<SDKValidationResult> ValidateSDKConfigurationAsync(ValidateSDKRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get SDK changelog
    /// </summary>
    /// <param name="language">Programming language</param>
    /// <param name="fromVersion">From version (optional)</param>
    /// <param name="toVersion">To version (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SDK changelog</returns>
    Task<SDKChangelog> GetSDKChangelogAsync(string language, string? fromVersion = null, string? toVersion = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate SDK migration guide
    /// </summary>
    /// <param name="request">Migration guide request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Migration guide</returns>
    Task<SDKMigrationGuide> GenerateMigrationGuideAsync(GenerateMigrationGuideRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get SDK performance benchmarks
    /// </summary>
    /// <param name="language">Programming language</param>
    /// <param name="version">SDK version</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Performance benchmarks</returns>
    Task<SDKPerformanceBenchmarks> GetPerformanceBenchmarksAsync(string language, string version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate interactive SDK playground
    /// </summary>
    /// <param name="request">Playground request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Playground configuration</returns>
    Task<SDKPlayground> GeneratePlaygroundAsync(GeneratePlaygroundRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get SDK support information
    /// </summary>
    /// <param name="language">Programming language</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Support information</returns>
    Task<SDKSupportInfo> GetSupportInfoAsync(string language, CancellationToken cancellationToken = default);
}
