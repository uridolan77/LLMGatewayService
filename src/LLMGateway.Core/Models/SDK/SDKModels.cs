namespace LLMGateway.Core.Models.SDK;

/// <summary>
/// SDK generation request
/// </summary>
public class GenerateSDKRequest
{
    /// <summary>
    /// Programming language
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// SDK version
    /// </summary>
    public string Version { get; set; } = "latest";

    /// <summary>
    /// Include features
    /// </summary>
    public List<string> IncludeFeatures { get; set; } = new();

    /// <summary>
    /// Package name
    /// </summary>
    public string? PackageName { get; set; }

    /// <summary>
    /// Namespace/module name
    /// </summary>
    public string? Namespace { get; set; }

    /// <summary>
    /// Custom configuration
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();
}

/// <summary>
/// Generated SDK information
/// </summary>
public class GeneratedSDK
{
    /// <summary>
    /// SDK ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Language
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Version
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Download URL
    /// </summary>
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>
    /// Documentation URL
    /// </summary>
    public string DocumentationUrl { get; set; } = string.Empty;

    /// <summary>
    /// Package information
    /// </summary>
    public PackageInfo PackageInfo { get; set; } = new();

    /// <summary>
    /// Installation instructions
    /// </summary>
    public string InstallationInstructions { get; set; } = string.Empty;

    /// <summary>
    /// Quick start guide
    /// </summary>
    public string QuickStartGuide { get; set; } = string.Empty;

    /// <summary>
    /// Generated at
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// Expires at
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// SDK language option
/// </summary>
public class SDKLanguageOption
{
    /// <summary>
    /// Language name
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Available versions
    /// </summary>
    public List<string> AvailableVersions { get; set; } = new();

    /// <summary>
    /// Latest version
    /// </summary>
    public string LatestVersion { get; set; } = string.Empty;

    /// <summary>
    /// Supported features
    /// </summary>
    public List<string> SupportedFeatures { get; set; } = new();

    /// <summary>
    /// Maturity level
    /// </summary>
    public string MaturityLevel { get; set; } = string.Empty;

    /// <summary>
    /// Minimum language version
    /// </summary>
    public string? MinimumLanguageVersion { get; set; }
}

/// <summary>
/// Package information
/// </summary>
public class PackageInfo
{
    /// <summary>
    /// Package name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Version
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Author
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// License
    /// </summary>
    public string License { get; set; } = string.Empty;

    /// <summary>
    /// Repository URL
    /// </summary>
    public string? RepositoryUrl { get; set; }

    /// <summary>
    /// Dependencies
    /// </summary>
    public List<Dependency> Dependencies { get; set; } = new();
}

/// <summary>
/// Package dependency
/// </summary>
public class Dependency
{
    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Version constraint
    /// </summary>
    public string VersionConstraint { get; set; } = string.Empty;

    /// <summary>
    /// Is optional
    /// </summary>
    public bool IsOptional { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// SDK documentation
/// </summary>
public class SDKDocumentation
{
    /// <summary>
    /// Language
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Version
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Getting started guide
    /// </summary>
    public string GettingStarted { get; set; } = string.Empty;

    /// <summary>
    /// API reference
    /// </summary>
    public string ApiReference { get; set; } = string.Empty;

    /// <summary>
    /// Examples
    /// </summary>
    public List<CodeExample> Examples { get; set; } = new();

    /// <summary>
    /// Tutorials
    /// </summary>
    public List<Tutorial> Tutorials { get; set; } = new();

    /// <summary>
    /// FAQ
    /// </summary>
    public List<FAQItem> FAQ { get; set; } = new();

    /// <summary>
    /// Changelog
    /// </summary>
    public string Changelog { get; set; } = string.Empty;
}

/// <summary>
/// Code example
/// </summary>
public class CodeExample
{
    /// <summary>
    /// Title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Code
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Language
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Category
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Tags
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Difficulty level
    /// </summary>
    public string DifficultyLevel { get; set; } = string.Empty;
}

/// <summary>
/// Tutorial
/// </summary>
public class Tutorial
{
    /// <summary>
    /// Title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Content
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Estimated duration
    /// </summary>
    public TimeSpan EstimatedDuration { get; set; }

    /// <summary>
    /// Prerequisites
    /// </summary>
    public List<string> Prerequisites { get; set; } = new();

    /// <summary>
    /// Learning objectives
    /// </summary>
    public List<string> LearningObjectives { get; set; } = new();
}

/// <summary>
/// FAQ item
/// </summary>
public class FAQItem
{
    /// <summary>
    /// Question
    /// </summary>
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// Answer
    /// </summary>
    public string Answer { get; set; } = string.Empty;

    /// <summary>
    /// Category
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Tags
    /// </summary>
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Custom API client generation request
/// </summary>
public class GenerateCustomClientRequest
{
    /// <summary>
    /// Language
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Client name
    /// </summary>
    public string ClientName { get; set; } = string.Empty;

    /// <summary>
    /// API endpoints to include
    /// </summary>
    public List<string> IncludeEndpoints { get; set; } = new();

    /// <summary>
    /// Authentication configuration
    /// </summary>
    public AuthenticationConfig AuthConfig { get; set; } = new();

    /// <summary>
    /// Custom headers
    /// </summary>
    public Dictionary<string, string> CustomHeaders { get; set; } = new();

    /// <summary>
    /// Base URL
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Timeout settings
    /// </summary>
    public TimeoutSettings TimeoutSettings { get; set; } = new();
}

/// <summary>
/// Authentication configuration
/// </summary>
public class AuthenticationConfig
{
    /// <summary>
    /// Authentication type
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// API key header name
    /// </summary>
    public string? ApiKeyHeader { get; set; }

    /// <summary>
    /// Bearer token support
    /// </summary>
    public bool SupportsBearerToken { get; set; }

    /// <summary>
    /// Custom authentication
    /// </summary>
    public Dictionary<string, object> CustomAuth { get; set; } = new();
}

/// <summary>
/// Timeout settings
/// </summary>
public class TimeoutSettings
{
    /// <summary>
    /// Connection timeout
    /// </summary>
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Request timeout
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Retry attempts
    /// </summary>
    public int RetryAttempts { get; set; } = 3;

    /// <summary>
    /// Retry delay
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
}

/// <summary>
/// Custom API client
/// </summary>
public class CustomAPIClient
{
    /// <summary>
    /// Client ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Client name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Language
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Generated code
    /// </summary>
    public string GeneratedCode { get; set; } = string.Empty;

    /// <summary>
    /// Usage instructions
    /// </summary>
    public string UsageInstructions { get; set; } = string.Empty;

    /// <summary>
    /// Dependencies
    /// </summary>
    public List<Dependency> Dependencies { get; set; } = new();

    /// <summary>
    /// Generated at
    /// </summary>
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// SDK usage analytics
/// </summary>
public class SDKUsageAnalytics
{
    /// <summary>
    /// Total downloads
    /// </summary>
    public long TotalDownloads { get; set; }

    /// <summary>
    /// Downloads by language
    /// </summary>
    public Dictionary<string, long> DownloadsByLanguage { get; set; } = new();

    /// <summary>
    /// Downloads by version
    /// </summary>
    public Dictionary<string, long> DownloadsByVersion { get; set; } = new();

    /// <summary>
    /// Active users
    /// </summary>
    public long ActiveUsers { get; set; }

    /// <summary>
    /// Popular features
    /// </summary>
    public List<string> PopularFeatures { get; set; } = new();

    /// <summary>
    /// Error reports
    /// </summary>
    public List<object> ErrorReports { get; set; } = new();

    /// <summary>
    /// Performance metrics
    /// </summary>
    public object PerformanceMetrics { get; set; } = new();
}

/// <summary>
/// SDK analytics request
/// </summary>
public class SDKAnalyticsRequest
{
    /// <summary>
    /// Start date
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Language filter
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Version filter
    /// </summary>
    public string? Version { get; set; }
}

/// <summary>
/// SDK validation request
/// </summary>
public class ValidateSDKRequest
{
    /// <summary>
    /// Language
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Configuration parameters
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();

    /// <summary>
    /// Features to validate
    /// </summary>
    public List<string> Features { get; set; } = new();
}

/// <summary>
/// SDK validation result
/// </summary>
public class SDKValidationResult
{
    /// <summary>
    /// Is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Validation errors
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Validation warnings
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Configuration suggestions
    /// </summary>
    public List<string> Suggestions { get; set; } = new();
}

/// <summary>
/// SDK changelog
/// </summary>
public class SDKChangelog
{
    /// <summary>
    /// Language
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// From version
    /// </summary>
    public string? FromVersion { get; set; }

    /// <summary>
    /// To version
    /// </summary>
    public string? ToVersion { get; set; }

    /// <summary>
    /// Changelog entries
    /// </summary>
    public List<ChangelogEntry> Entries { get; set; } = new();
}

/// <summary>
/// Changelog entry
/// </summary>
public class ChangelogEntry
{
    /// <summary>
    /// Version
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Release date
    /// </summary>
    public DateTime ReleaseDate { get; set; }

    /// <summary>
    /// Changes
    /// </summary>
    public List<ChangeItem> Changes { get; set; } = new();

    /// <summary>
    /// Breaking changes
    /// </summary>
    public List<string> BreakingChanges { get; set; } = new();
}

/// <summary>
/// Change item
/// </summary>
public class ChangeItem
{
    /// <summary>
    /// Type (added, changed, deprecated, removed, fixed, security)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Issue reference
    /// </summary>
    public string? IssueReference { get; set; }
}

/// <summary>
/// SDK migration guide request
/// </summary>
public class GenerateMigrationGuideRequest
{
    /// <summary>
    /// Language
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// From version
    /// </summary>
    public string FromVersion { get; set; } = string.Empty;

    /// <summary>
    /// To version
    /// </summary>
    public string ToVersion { get; set; } = string.Empty;

    /// <summary>
    /// Include code examples
    /// </summary>
    public bool IncludeCodeExamples { get; set; } = true;
}

/// <summary>
/// SDK migration guide
/// </summary>
public class SDKMigrationGuide
{
    /// <summary>
    /// Language
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// From version
    /// </summary>
    public string FromVersion { get; set; } = string.Empty;

    /// <summary>
    /// To version
    /// </summary>
    public string ToVersion { get; set; } = string.Empty;

    /// <summary>
    /// Migration steps
    /// </summary>
    public List<MigrationStep> Steps { get; set; } = new();

    /// <summary>
    /// Breaking changes
    /// </summary>
    public List<BreakingChange> BreakingChanges { get; set; } = new();

    /// <summary>
    /// Code examples
    /// </summary>
    public List<MigrationExample> Examples { get; set; } = new();
}

/// <summary>
/// Migration step
/// </summary>
public class MigrationStep
{
    /// <summary>
    /// Step number
    /// </summary>
    public int StepNumber { get; set; }

    /// <summary>
    /// Title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Is required
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Estimated time
    /// </summary>
    public TimeSpan? EstimatedTime { get; set; }
}

/// <summary>
/// Breaking change
/// </summary>
public class BreakingChange
{
    /// <summary>
    /// Component affected
    /// </summary>
    public string Component { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Migration action
    /// </summary>
    public string MigrationAction { get; set; } = string.Empty;

    /// <summary>
    /// Impact level (low, medium, high)
    /// </summary>
    public string ImpactLevel { get; set; } = string.Empty;
}

/// <summary>
/// Migration example
/// </summary>
public class MigrationExample
{
    /// <summary>
    /// Title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Before code
    /// </summary>
    public string BeforeCode { get; set; } = string.Empty;

    /// <summary>
    /// After code
    /// </summary>
    public string AfterCode { get; set; } = string.Empty;

    /// <summary>
    /// Explanation
    /// </summary>
    public string Explanation { get; set; } = string.Empty;
}

/// <summary>
/// SDK performance benchmarks
/// </summary>
public class SDKPerformanceBenchmarks
{
    /// <summary>
    /// Language
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Version
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Benchmark results
    /// </summary>
    public List<BenchmarkResult> Results { get; set; } = new();

    /// <summary>
    /// Test environment
    /// </summary>
    public TestEnvironment Environment { get; set; } = new();
}

/// <summary>
/// Benchmark result
/// </summary>
public class BenchmarkResult
{
    /// <summary>
    /// Test name
    /// </summary>
    public string TestName { get; set; } = string.Empty;

    /// <summary>
    /// Average response time (ms)
    /// </summary>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// Throughput (requests/second)
    /// </summary>
    public double Throughput { get; set; }

    /// <summary>
    /// Memory usage (MB)
    /// </summary>
    public double MemoryUsage { get; set; }

    /// <summary>
    /// CPU usage (%)
    /// </summary>
    public double CpuUsage { get; set; }

    /// <summary>
    /// Success rate (%)
    /// </summary>
    public double SuccessRate { get; set; }
}

/// <summary>
/// Test environment
/// </summary>
public class TestEnvironment
{
    /// <summary>
    /// Operating system
    /// </summary>
    public string OperatingSystem { get; set; } = string.Empty;

    /// <summary>
    /// Runtime version
    /// </summary>
    public string RuntimeVersion { get; set; } = string.Empty;

    /// <summary>
    /// Hardware specifications
    /// </summary>
    public string HardwareSpecs { get; set; } = string.Empty;

    /// <summary>
    /// Test date
    /// </summary>
    public DateTime TestDate { get; set; }
}

/// <summary>
/// SDK playground request
/// </summary>
public class GeneratePlaygroundRequest
{
    /// <summary>
    /// Language
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Features to include
    /// </summary>
    public List<string> Features { get; set; } = new();

    /// <summary>
    /// Theme
    /// </summary>
    public string Theme { get; set; } = "default";

    /// <summary>
    /// Include examples
    /// </summary>
    public bool IncludeExamples { get; set; } = true;
}

/// <summary>
/// SDK playground
/// </summary>
public class SDKPlayground
{
    /// <summary>
    /// Playground ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Language
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Playground URL
    /// </summary>
    public string PlaygroundUrl { get; set; } = string.Empty;

    /// <summary>
    /// Embed code
    /// </summary>
    public string EmbedCode { get; set; } = string.Empty;

    /// <summary>
    /// Available examples
    /// </summary>
    public List<PlaygroundExample> Examples { get; set; } = new();

    /// <summary>
    /// Configuration
    /// </summary>
    public PlaygroundConfig Configuration { get; set; } = new();
}

/// <summary>
/// Playground example
/// </summary>
public class PlaygroundExample
{
    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Code
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Category
    /// </summary>
    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// Playground configuration
/// </summary>
public class PlaygroundConfig
{
    /// <summary>
    /// Theme
    /// </summary>
    public string Theme { get; set; } = string.Empty;

    /// <summary>
    /// Editor settings
    /// </summary>
    public Dictionary<string, object> EditorSettings { get; set; } = new();

    /// <summary>
    /// Available features
    /// </summary>
    public List<string> AvailableFeatures { get; set; } = new();
}

/// <summary>
/// SDK support information
/// </summary>
public class SDKSupportInfo
{
    /// <summary>
    /// Language
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Support level
    /// </summary>
    public string SupportLevel { get; set; } = string.Empty;

    /// <summary>
    /// Documentation links
    /// </summary>
    public List<SupportLink> DocumentationLinks { get; set; } = new();

    /// <summary>
    /// Community resources
    /// </summary>
    public List<SupportLink> CommunityResources { get; set; } = new();

    /// <summary>
    /// Known issues
    /// </summary>
    public List<KnownIssue> KnownIssues { get; set; } = new();

    /// <summary>
    /// Contact information
    /// </summary>
    public ContactInfo ContactInfo { get; set; } = new();
}

/// <summary>
/// Support link
/// </summary>
public class SupportLink
{
    /// <summary>
    /// Title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Type
    /// </summary>
    public string Type { get; set; } = string.Empty;
}

/// <summary>
/// Known issue
/// </summary>
public class KnownIssue
{
    /// <summary>
    /// Title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Severity
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// Workaround
    /// </summary>
    public string? Workaround { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Contact information
/// </summary>
public class ContactInfo
{
    /// <summary>
    /// Support email
    /// </summary>
    public string? SupportEmail { get; set; }

    /// <summary>
    /// Community forum URL
    /// </summary>
    public string? CommunityForumUrl { get; set; }

    /// <summary>
    /// GitHub repository URL
    /// </summary>
    public string? GitHubUrl { get; set; }

    /// <summary>
    /// Discord server URL
    /// </summary>
    public string? DiscordUrl { get; set; }
}
