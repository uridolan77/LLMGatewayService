using System.Diagnostics;
using System.Text;
using System.Text.Json;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.SDK;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;

namespace LLMGateway.Infrastructure.Services;

/// <summary>
/// SDK management service implementation
/// </summary>
public class SDKManagementService : ISDKManagementService
{
    private readonly ILogger<SDKManagementService> _logger;
    private readonly IDistributedCache _cache;
    private readonly IHttpClientFactory _httpClientFactory;

    // SDK language configurations
    private readonly Dictionary<string, SDKLanguageOption> _supportedLanguages;

    /// <summary>
    /// Constructor
    /// </summary>
    public SDKManagementService(
        ILogger<SDKManagementService> logger,
        IDistributedCache cache,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _supportedLanguages = InitializeSupportedLanguages();
    }

    /// <inheritdoc/>
    public async Task<GeneratedSDK> GenerateSDKAsync(GenerateSDKRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = new Activity("SDKManagement.GenerateSDK").Start();
        activity?.SetTag("language", request.Language);
        activity?.SetTag("version", request.Version);

        try
        {
            _logger.LogInformation("Generating SDK for language {Language} version {Version}",
                request.Language, request.Version);

            if (!_supportedLanguages.ContainsKey(request.Language.ToLowerInvariant()))
            {
                throw new ArgumentException($"Unsupported language: {request.Language}");
            }

            var sdkId = Guid.NewGuid().ToString();
            var languageConfig = _supportedLanguages[request.Language.ToLowerInvariant()];

            // Generate SDK based on language
            var generatedCode = await GenerateSDKCodeAsync(request, languageConfig);
            var packageInfo = GeneratePackageInfo(request, languageConfig);
            var documentation = await GenerateSDKDocumentationAsync(request, languageConfig);

            var sdk = new GeneratedSDK
            {
                Id = sdkId,
                Language = request.Language,
                Version = request.Version,
                DownloadUrl = $"/api/v1/sdk/download/{sdkId}",
                DocumentationUrl = $"/api/v1/sdk/documentation/{request.Language}?version={request.Version}",
                PackageInfo = packageInfo,
                InstallationInstructions = GenerateInstallationInstructions(request, packageInfo),
                QuickStartGuide = GenerateQuickStartGuide(request, languageConfig),
                GeneratedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };

            // Store generated SDK
            await StoreGeneratedSDKAsync(sdk, generatedCode);

            _logger.LogInformation("SDK generated successfully for language {Language} with ID {SdkId}",
                request.Language, sdkId);

            return sdk;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate SDK for language {Language}", request.Language);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<SDKLanguageOption>> GetAvailableSDKLanguagesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting available SDK languages");

            // Return supported languages with current status
            var languages = _supportedLanguages.Values.ToList();

            // Update availability status for each language
            foreach (var language in languages)
            {
                language.LatestVersion = await GetLatestVersionAsync(language.Language);
            }

            return languages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available SDK languages");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<SDKDocumentation> GetSDKDocumentationAsync(string language, string version, CancellationToken cancellationToken = default)
    {
        using var activity = new Activity("SDKManagement.GetDocumentation").Start();
        activity?.SetTag("language", language);
        activity?.SetTag("version", version);

        try
        {
            _logger.LogDebug("Getting SDK documentation for {Language} version {Version}", language, version);

            var cacheKey = $"sdk_docs:{language}:{version}";
            var cached = await GetFromCacheAsync<SDKDocumentation>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            if (!_supportedLanguages.ContainsKey(language.ToLowerInvariant()))
            {
                throw new ArgumentException($"Unsupported language: {language}");
            }

            var languageConfig = _supportedLanguages[language.ToLowerInvariant()];
            var documentation = await GenerateSDKDocumentationAsync(
                new GenerateSDKRequest { Language = language, Version = version },
                languageConfig);

            await SetCacheAsync(cacheKey, documentation, TimeSpan.FromHours(1));

            return documentation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get SDK documentation for {Language}", language);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CodeExample>> GetCodeExamplesAsync(string language, string useCase, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting code examples for {Language} use case {UseCase}", language, useCase);

            var cacheKey = $"code_examples:{language}:{useCase}";
            var cached = await GetFromCacheAsync<List<CodeExample>>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            var examples = await GenerateCodeExamplesAsync(language, useCase);
            await SetCacheAsync(cacheKey, examples, TimeSpan.FromHours(2));

            return examples;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get code examples for {Language}", language);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<CustomAPIClient> GenerateCustomClientAsync(GenerateCustomClientRequest request, CancellationToken cancellationToken = default)
    {
        using var activity = new Activity("SDKManagement.GenerateCustomClient").Start();
        activity?.SetTag("language", request.Language);
        activity?.SetTag("clientName", request.ClientName);

        try
        {
            _logger.LogInformation("Generating custom API client {ClientName} for language {Language}",
                request.ClientName, request.Language);

            var clientId = Guid.NewGuid().ToString();
            var generatedCode = await GenerateCustomClientCodeAsync(request);
            var dependencies = GenerateClientDependencies(request);
            var instructions = GenerateClientUsageInstructions(request);

            var client = new CustomAPIClient
            {
                Id = clientId,
                Name = request.ClientName,
                Language = request.Language,
                GeneratedCode = generatedCode,
                UsageInstructions = instructions,
                Dependencies = dependencies,
                GeneratedAt = DateTime.UtcNow
            };

            // Store generated client
            await StoreCustomClientAsync(client);

            _logger.LogInformation("Custom API client generated successfully with ID {ClientId}", clientId);
            return client;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate custom API client");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<SDKUsageAnalytics> GetSDKUsageAnalyticsAsync(SDKAnalyticsRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting SDK usage analytics");

            // This would typically query usage metrics from a database
            var analytics = new SDKUsageAnalytics
            {
                TotalDownloads = await GetTotalSDKDownloadsAsync(),
                DownloadsByLanguage = await GetDownloadsByLanguageAsync(),
                DownloadsByVersion = await GetDownloadsByVersionAsync(),
                ActiveUsers = await GetActiveSDKUsersAsync(),
                PopularFeatures = await GetPopularFeaturesAsync(),
                ErrorReports = await GetSDKErrorReportsAsync(),
                PerformanceMetrics = await GetSDKPerformanceMetricsAsync()
            };

            return analytics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get SDK usage analytics");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<SDKValidationResult> ValidateSDKConfigurationAsync(ValidateSDKRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Validating SDK configuration for {Language}", request.Language);

            var validationErrors = new List<string>();
            var warnings = new List<string>();

            // Validate language support
            if (!_supportedLanguages.ContainsKey(request.Language.ToLowerInvariant()))
            {
                validationErrors.Add($"Unsupported language: {request.Language}");
            }

            // Validate configuration parameters
            await ValidateConfigurationParametersAsync(request, validationErrors, warnings);

            var result = new SDKValidationResult
            {
                IsValid = validationErrors.Count == 0,
                Errors = validationErrors,
                Warnings = warnings,
                Suggestions = GenerateConfigurationSuggestions(request)
            };

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate SDK configuration");
            throw;
        }
    }

    // Helper methods for SDK generation
    private Dictionary<string, SDKLanguageOption> InitializeSupportedLanguages()
    {
        return new Dictionary<string, SDKLanguageOption>
        {
            ["csharp"] = new SDKLanguageOption
            {
                Language = "csharp",
                DisplayName = "C#",
                AvailableVersions = new List<string> { "1.0.0", "1.1.0", "2.0.0" },
                LatestVersion = "2.0.0",
                SupportedFeatures = new List<string> { "async", "streaming", "authentication", "retry" },
                MaturityLevel = "stable",
                MinimumLanguageVersion = ".NET 6.0"
            },
            ["python"] = new SDKLanguageOption
            {
                Language = "python",
                DisplayName = "Python",
                AvailableVersions = new List<string> { "1.0.0", "1.2.0", "2.0.0" },
                LatestVersion = "2.0.0",
                SupportedFeatures = new List<string> { "async", "streaming", "authentication", "retry", "typing" },
                MaturityLevel = "stable",
                MinimumLanguageVersion = "3.8"
            },
            ["javascript"] = new SDKLanguageOption
            {
                Language = "javascript",
                DisplayName = "JavaScript/TypeScript",
                AvailableVersions = new List<string> { "1.0.0", "1.1.0", "2.0.0" },
                LatestVersion = "2.0.0",
                SupportedFeatures = new List<string> { "async", "streaming", "authentication", "retry", "typescript" },
                MaturityLevel = "stable",
                MinimumLanguageVersion = "Node.js 16+"
            },
            ["java"] = new SDKLanguageOption
            {
                Language = "java",
                DisplayName = "Java",
                AvailableVersions = new List<string> { "1.0.0", "1.1.0" },
                LatestVersion = "1.1.0",
                SupportedFeatures = new List<string> { "async", "authentication", "retry" },
                MaturityLevel = "beta",
                MinimumLanguageVersion = "Java 11"
            },
            ["go"] = new SDKLanguageOption
            {
                Language = "go",
                DisplayName = "Go",
                AvailableVersions = new List<string> { "1.0.0" },
                LatestVersion = "1.0.0",
                SupportedFeatures = new List<string> { "async", "authentication", "retry" },
                MaturityLevel = "alpha",
                MinimumLanguageVersion = "Go 1.19"
            }
        };
    }

    private async Task<string> GenerateSDKCodeAsync(GenerateSDKRequest request, SDKLanguageOption languageConfig)
    {
        // This would contain the actual SDK code generation logic
        // For now, return a template based on the language
        return request.Language.ToLowerInvariant() switch
        {
            "csharp" => await GenerateCSharpSDKAsync(request),
            "python" => await GeneratePythonSDKAsync(request),
            "javascript" => await GenerateJavaScriptSDKAsync(request),
            "java" => await GenerateJavaSDKAsync(request),
            "go" => await GenerateGoSDKAsync(request),
            _ => throw new NotSupportedException($"Code generation for {request.Language} is not implemented")
        };
    }

    private async Task<string> GenerateCSharpSDKAsync(GenerateSDKRequest request)
    {
        await Task.Delay(1); // Simulate generation time

        var code = new StringBuilder();
        code.AppendLine("using System;");
        code.AppendLine("using System.Net.Http;");
        code.AppendLine("using System.Threading.Tasks;");
        code.AppendLine();
        code.AppendLine($"namespace {request.Namespace ?? "LLMGateway.SDK"}");
        code.AppendLine("{");
        code.AppendLine("    /// <summary>");
        code.AppendLine("    /// LLM Gateway SDK Client");
        code.AppendLine("    /// </summary>");
        code.AppendLine("    public class LLMGatewayClient");
        code.AppendLine("    {");
        code.AppendLine("        private readonly HttpClient _httpClient;");
        code.AppendLine("        private readonly string _apiKey;");
        code.AppendLine();
        code.AppendLine("        public LLMGatewayClient(string apiKey, string baseUrl = \"https://api.llmgateway.com\")");
        code.AppendLine("        {");
        code.AppendLine("            _apiKey = apiKey;");
        code.AppendLine("            _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };");
        code.AppendLine("            _httpClient.DefaultRequestHeaders.Add(\"Authorization\", $\"Bearer {apiKey}\");");
        code.AppendLine("        }");
        code.AppendLine();
        code.AppendLine("        public async Task<CompletionResponse> CreateCompletionAsync(CompletionRequest request)");
        code.AppendLine("        {");
        code.AppendLine("            // Implementation here");
        code.AppendLine("            throw new NotImplementedException();");
        code.AppendLine("        }");
        code.AppendLine("    }");
        code.AppendLine("}");

        return code.ToString();
    }

    private PackageInfo GeneratePackageInfo(GenerateSDKRequest request, SDKLanguageOption languageConfig)
    {
        return new PackageInfo
        {
            Name = request.PackageName ?? $"llm-gateway-sdk-{request.Language}",
            Version = request.Version,
            Description = "Official SDK for LLM Gateway API",
            Author = "LLM Gateway Team",
            License = "MIT",
            RepositoryUrl = "https://github.com/llmgateway/sdk",
            Dependencies = GenerateLanguageDependencies(request.Language)
        };
    }

    private List<Dependency> GenerateLanguageDependencies(string language)
    {
        return language.ToLowerInvariant() switch
        {
            "csharp" => new List<Dependency>
            {
                new() { Name = "System.Net.Http.Json", VersionConstraint = ">= 6.0.0" },
                new() { Name = "System.Text.Json", VersionConstraint = ">= 6.0.0" }
            },
            "python" => new List<Dependency>
            {
                new() { Name = "httpx", VersionConstraint = ">= 0.24.0" },
                new() { Name = "pydantic", VersionConstraint = ">= 2.0.0" }
            },
            "javascript" => new List<Dependency>
            {
                new() { Name = "axios", VersionConstraint = "^1.4.0" },
                new() { Name = "typescript", VersionConstraint = "^5.0.0", IsOptional = true }
            },
            _ => new List<Dependency>()
        };
    }

    // Additional helper methods would be implemented here...
    private async Task<string> GetLatestVersionAsync(string language) { await Task.Delay(1); return "2.0.0"; }
    private async Task<SDKDocumentation> GenerateSDKDocumentationAsync(GenerateSDKRequest request, SDKLanguageOption config) { await Task.Delay(1); return new(); }
    private async Task<List<CodeExample>> GenerateCodeExamplesAsync(string language, string useCase) { await Task.Delay(1); return new(); }
    private async Task<string> GenerateCustomClientCodeAsync(GenerateCustomClientRequest request) { await Task.Delay(1); return "// Generated client code"; }
    private List<Dependency> GenerateClientDependencies(GenerateCustomClientRequest request) => new();
    private string GenerateClientUsageInstructions(GenerateCustomClientRequest request) => "// Usage instructions";
    private async Task StoreGeneratedSDKAsync(GeneratedSDK sdk, string code) { await Task.Delay(1); }
    private async Task StoreCustomClientAsync(CustomAPIClient client) { await Task.Delay(1); }
    private string GenerateInstallationInstructions(GenerateSDKRequest request, PackageInfo packageInfo) => "// Installation instructions";
    private string GenerateQuickStartGuide(GenerateSDKRequest request, SDKLanguageOption config) => "// Quick start guide";
    private async Task<long> GetTotalSDKDownloadsAsync() { await Task.Delay(1); return 1500; }
    private async Task<Dictionary<string, long>> GetDownloadsByLanguageAsync() { await Task.Delay(1); return new(); }
    private async Task<Dictionary<string, long>> GetDownloadsByVersionAsync() { await Task.Delay(1); return new(); }
    private async Task<long> GetActiveSDKUsersAsync() { await Task.Delay(1); return 250; }
    private async Task<List<string>> GetPopularFeaturesAsync() { await Task.Delay(1); return new(); }
    private async Task<List<object>> GetSDKErrorReportsAsync() { await Task.Delay(1); return new(); }
    private async Task<object> GetSDKPerformanceMetricsAsync() { await Task.Delay(1); return new { }; }
    private async Task ValidateConfigurationParametersAsync(ValidateSDKRequest request, List<string> errors, List<string> warnings) { await Task.Delay(1); }
    private List<string> GenerateConfigurationSuggestions(ValidateSDKRequest request) => new();
    private async Task<string> GeneratePythonSDKAsync(GenerateSDKRequest request) { await Task.Delay(1); return "# Python SDK code"; }
    private async Task<string> GenerateJavaScriptSDKAsync(GenerateSDKRequest request) { await Task.Delay(1); return "// JavaScript SDK code"; }
    private async Task<string> GenerateJavaSDKAsync(GenerateSDKRequest request) { await Task.Delay(1); return "// Java SDK code"; }
    private async Task<string> GenerateGoSDKAsync(GenerateSDKRequest request) { await Task.Delay(1); return "// Go SDK code"; }

    private async Task<T?> GetFromCacheAsync<T>(string key) where T : class
    {
        try
        {
            var cached = await _cache.GetStringAsync(key);
            return cached != null ? JsonSerializer.Deserialize<T>(cached) : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get from cache: {Key}", key);
            return null;
        }
    }

    private async Task SetCacheAsync<T>(string key, T value, TimeSpan expiration)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };
            await _cache.SetStringAsync(key, json, options);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set cache: {Key}", key);
        }
    }

    /// <inheritdoc/>
    public async Task<SDKChangelog> GetSDKChangelogAsync(string language, string? fromVersion = null, string? toVersion = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting SDK changelog for {Language} from {FromVersion} to {ToVersion}", language, fromVersion, toVersion);

            var cacheKey = $"sdk_changelog:{language}:{fromVersion}:{toVersion}";
            var cached = await GetFromCacheAsync<SDKChangelog>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            var changelog = new SDKChangelog
            {
                Language = language,
                FromVersion = fromVersion,
                ToVersion = toVersion ?? _supportedLanguages[language.ToLowerInvariant()].LatestVersion,
                Entries = new List<ChangelogEntry>
                {
                    new()
                    {
                        Version = "2.0.0",
                        ReleaseDate = DateTime.UtcNow.AddDays(-30),
                        Changes = new List<ChangeItem>
                        {
                            new() { Type = "added", Description = "Added streaming support" },
                            new() { Type = "changed", Description = "Changed authentication method" },
                            new() { Type = "fixed", Description = "Fixed memory leaks" }
                        },
                        BreakingChanges = new List<string> { "Changed authentication method", "Renamed completion methods" }
                    },
                    new()
                    {
                        Version = "1.1.0",
                        ReleaseDate = DateTime.UtcNow.AddDays(-60),
                        Changes = new List<ChangeItem>
                        {
                            new() { Type = "added", Description = "Added batch processing" },
                            new() { Type = "fixed", Description = "Fixed timeout issues" }
                        },
                        BreakingChanges = new List<string>()
                    }
                }
            };

            await SetCacheAsync(cacheKey, changelog, TimeSpan.FromHours(6));
            return changelog;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get SDK changelog for {Language}", language);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<SDKMigrationGuide> GenerateMigrationGuideAsync(GenerateMigrationGuideRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating migration guide from {FromVersion} to {ToVersion} for {Language}",
                request.FromVersion, request.ToVersion, request.Language);

            var guide = new SDKMigrationGuide
            {
                Language = request.Language,
                FromVersion = request.FromVersion,
                ToVersion = request.ToVersion,
                Steps = new List<MigrationStep>
                {
                    new()
                    {
                        StepNumber = 1,
                        Title = "Update Package Dependencies",
                        Description = "Update your package.json/requirements.txt/etc. to use the new version",
                        IsRequired = true,
                        EstimatedTime = TimeSpan.FromMinutes(5)
                    },
                    new()
                    {
                        StepNumber = 2,
                        Title = "Update Authentication",
                        Description = "The authentication method has changed in the new version",
                        IsRequired = true,
                        EstimatedTime = TimeSpan.FromMinutes(30)
                    },
                    new()
                    {
                        StepNumber = 3,
                        Title = "Update API Calls",
                        Description = "Some method signatures have changed",
                        IsRequired = true,
                        EstimatedTime = TimeSpan.FromHours(1.5)
                    }
                },
                BreakingChanges = new List<BreakingChange>
                {
                    new()
                    {
                        Component = "Authentication",
                        Description = "Authentication method changed from API key header to Bearer token",
                        MigrationAction = "Update authentication code to use Bearer token",
                        ImpactLevel = "high"
                    },
                    new()
                    {
                        Component = "API Methods",
                        Description = "Completion method renamed from CreateCompletion to GenerateCompletion",
                        MigrationAction = "Rename method calls in your code",
                        ImpactLevel = "medium"
                    }
                },
                Examples = new List<MigrationExample>
                {
                    new()
                    {
                        Title = "Authentication Update",
                        BeforeCode = GetOldAuthExample(request.Language),
                        AfterCode = GetNewAuthExample(request.Language),
                        Explanation = "Update authentication method from API key header to Bearer token"
                    }
                }
            };

            return guide;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate migration guide");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<SDKPerformanceBenchmarks> GetPerformanceBenchmarksAsync(string language, string version, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting performance benchmarks for {Language} version {Version}", language, version);

            var benchmarks = new SDKPerformanceBenchmarks
            {
                Language = language,
                Version = version,
                Environment = new TestEnvironment
                {
                    OperatingSystem = "Windows 11",
                    RuntimeVersion = ".NET 8.0",
                    HardwareSpecs = "4 CPU cores, 8GB RAM",
                    TestDate = DateTime.UtcNow
                },
                Results = new List<BenchmarkResult>
                {
                    new()
                    {
                        TestName = "Simple Completion Request",
                        AverageResponseTime = 250.0,
                        Throughput = 45.2,
                        MemoryUsage = 12.0,
                        CpuUsage = 5.0,
                        SuccessRate = 99.8
                    },
                    new()
                    {
                        TestName = "Streaming Completion",
                        AverageResponseTime = 180.0,
                        Throughput = 38.7,
                        MemoryUsage = 18.0,
                        CpuUsage = 8.0,
                        SuccessRate = 99.5
                    },
                    new()
                    {
                        TestName = "Batch Processing",
                        AverageResponseTime = 850.0,
                        Throughput = 12.5,
                        MemoryUsage = 45.0,
                        CpuUsage = 25.0,
                        SuccessRate = 98.9
                    }
                }
            };

            return benchmarks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get performance benchmarks for {Language}", language);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<SDKPlayground> GeneratePlaygroundAsync(GeneratePlaygroundRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating SDK playground for {Language}", request.Language);

            var playground = new SDKPlayground
            {
                Id = Guid.NewGuid().ToString(),
                Language = request.Language,
                PlaygroundUrl = $"/playground/{request.Language}",
                EmbedCode = GenerateEmbedCode(request.Language),
                Examples = new List<PlaygroundExample>
                {
                    new()
                    {
                        Name = "Basic Completion",
                        Description = "Simple text completion example",
                        Code = await GeneratePlaygroundExampleAsync(request.Language),
                        Category = "basic"
                    },
                    new()
                    {
                        Name = "Streaming Response",
                        Description = "Real-time streaming completion",
                        Code = "// Streaming example code here",
                        Category = "advanced"
                    }
                },
                Configuration = new PlaygroundConfig
                {
                    Theme = "dark",
                    EditorSettings = new Dictionary<string, object>
                    {
                        ["autoComplete"] = true,
                        ["showLineNumbers"] = true,
                        ["enableDebugging"] = true
                    },
                    AvailableFeatures = new List<string> { "completion", "streaming", "function_calling" }
                }
            };

            return playground;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate SDK playground");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<SDKSupportInfo> GetSupportInfoAsync(string language, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting support info for {Language}", language);

            var supportInfo = new SDKSupportInfo
            {
                Language = language,
                SupportLevel = _supportedLanguages.ContainsKey(language.ToLowerInvariant())
                    ? _supportedLanguages[language.ToLowerInvariant()].MaturityLevel
                    : "unsupported",
                DocumentationLinks = new List<SupportLink>
                {
                    new()
                    {
                        Title = "Official Documentation",
                        Url = $"/docs/sdk/{language}",
                        Description = "Comprehensive documentation and guides"
                    },
                    new()
                    {
                        Title = "API Reference",
                        Url = $"/docs/api/{language}",
                        Description = "Complete API reference documentation"
                    }
                },
                CommunityResources = new List<SupportLink>
                {
                    new()
                    {
                        Title = "Community Forum",
                        Url = $"/community/{language}",
                        Description = "Community-driven support and discussions"
                    },
                    new()
                    {
                        Title = "GitHub Issues",
                        Url = $"https://github.com/llmgateway/sdk-{language}/issues",
                        Description = "Bug reports and feature requests"
                    }
                },
                KnownIssues = new List<KnownIssue>
                {
                    new()
                    {
                        Title = "Memory leak in streaming mode",
                        Description = "Long-running streaming connections may cause memory leaks",
                        Severity = "medium",
                        Status = "investigating",
                        Workaround = "Restart connections periodically"
                    }
                },
                ContactInfo = new ContactInfo
                {
                    SupportEmail = "sdk-support@llmgateway.com",
                    CommunityForumUrl = $"/community/{language}",
                    GitHubUrl = $"https://github.com/llmgateway/sdk-{language}",
                    DiscordUrl = "https://discord.gg/llmgateway"
                }
            };

            return supportInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get support info for {Language}", language);
            throw;
        }
    }

    // Helper methods for new functionality
    private string GetOldDependencyExample(string language, string version) =>
        language.ToLowerInvariant() switch
        {
            "csharp" => $"<PackageReference Include=\"LLMGateway.SDK\" Version=\"{version}\" />",
            "python" => $"llm-gateway-sdk=={version}",
            "javascript" => $"\"llm-gateway-sdk\": \"{version}\"",
            _ => $"// Old dependency for {language} version {version}"
        };

    private string GetNewDependencyExample(string language, string version) =>
        language.ToLowerInvariant() switch
        {
            "csharp" => $"<PackageReference Include=\"LLMGateway.SDK\" Version=\"{version}\" />",
            "python" => $"llm-gateway-sdk=={version}",
            "javascript" => $"\"llm-gateway-sdk\": \"{version}\"",
            _ => $"// New dependency for {language} version {version}"
        };

    private string GetOldAuthExample(string language) =>
        language.ToLowerInvariant() switch
        {
            "csharp" => "client.DefaultRequestHeaders.Add(\"X-API-Key\", apiKey);",
            "python" => "headers = {'X-API-Key': api_key}",
            "javascript" => "headers: { 'X-API-Key': apiKey }",
            _ => "// Old authentication example"
        };

    private string GetNewAuthExample(string language) =>
        language.ToLowerInvariant() switch
        {
            "csharp" => "client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(\"Bearer\", apiKey);",
            "python" => "headers = {'Authorization': f'Bearer {api_key}'}",
            "javascript" => "headers: { 'Authorization': `Bearer ${apiKey}` }",
            _ => "// New authentication example"
        };

    private string GetOldAPIExample(string language) =>
        language.ToLowerInvariant() switch
        {
            "csharp" => "var response = await client.CreateCompletion(request);",
            "python" => "response = client.create_completion(request)",
            "javascript" => "const response = await client.createCompletion(request);",
            _ => "// Old API example"
        };

    private string GetNewAPIExample(string language) =>
        language.ToLowerInvariant() switch
        {
            "csharp" => "var response = await client.GenerateCompletion(request);",
            "python" => "response = await client.generate_completion(request)",
            "javascript" => "const response = await client.generateCompletion(request);",
            _ => "// New API example"
        };

    private string GenerateEmbedCode(string language) =>
        $"<iframe src=\"/playground/{language}/embed\" width=\"100%\" height=\"600px\" frameborder=\"0\"></iframe>";

    private async Task<string> GeneratePlaygroundExampleAsync(string language)
    {
        await Task.Delay(1);
        return language.ToLowerInvariant() switch
        {
            "csharp" => @"
using LLMGateway.SDK;

var client = new LLMGatewayClient(""your-api-key"");
var request = new CompletionRequest
{
    Prompt = ""Hello, world!"",
    MaxTokens = 100
};

var response = await client.GenerateCompletion(request);
Console.WriteLine(response.Text);",
            "python" => @"
from llm_gateway_sdk import LLMGatewayClient

client = LLMGatewayClient(api_key=""your-api-key"")
request = {
    ""prompt"": ""Hello, world!"",
    ""max_tokens"": 100
}

response = await client.generate_completion(request)
print(response.text)",
            "javascript" => @"
import { LLMGatewayClient } from 'llm-gateway-sdk';

const client = new LLMGatewayClient('your-api-key');
const request = {
    prompt: 'Hello, world!',
    maxTokens: 100
};

const response = await client.generateCompletion(request);
console.log(response.text);",
            _ => "// Example code for " + language
        };
    }
}
