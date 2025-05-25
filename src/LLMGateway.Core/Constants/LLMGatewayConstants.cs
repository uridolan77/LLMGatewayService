namespace LLMGateway.Core.Constants;

/// <summary>
/// Constants used throughout the LLM Gateway application
/// </summary>
public static class LLMGatewayConstants
{
    /// <summary>
    /// Cache key prefixes and patterns
    /// </summary>
    public static class CacheKeys
    {
        public const string ModelPrefix = "models:";
        public const string UserPrefix = "user:";
        public const string CompletionPrefix = "completion:";
        public const string EmbeddingPrefix = "embedding:";
        public const string AllModelsKey = "models:all";
        public const string ProviderHealthPrefix = "health:";
        public const string TokenUsagePrefix = "tokens:";
        public const string RateLimitPrefix = "rate_limit:";
    }

    /// <summary>
    /// Default values used throughout the application
    /// </summary>
    public static class Defaults
    {
        public const int TokensPerCharacter = 4;
        public const int MaxRetries = 3;
        public const int CacheExpirationMinutes = 60;
        public const int CircuitBreakerFailureThreshold = 5;
        public const int CircuitBreakerTimeoutMinutes = 1;
        public const int DefaultMaxTokens = 1000;
        public const double DefaultTemperature = 0.7;
        public const int DefaultTimeoutSeconds = 30;
        public const int RateLimitTokenLimit = 1000;
        public const int RateLimitReplenishmentPeriodSeconds = 60;
        public const int RateLimitTokensPerPeriod = 100;
        public const int RateLimitQueueLimit = 100;
    }

    /// <summary>
    /// Error codes for consistent error handling
    /// </summary>
    public static class ErrorCodes
    {
        public const string ModelNotFound = "MODEL_NOT_FOUND";
        public const string ProviderUnavailable = "PROVIDER_UNAVAILABLE";
        public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";
        public const string ContentFiltered = "CONTENT_FILTERED";
        public const string InvalidRequest = "INVALID_REQUEST";
        public const string AuthenticationFailed = "AUTHENTICATION_FAILED";
        public const string InsufficientQuota = "INSUFFICIENT_QUOTA";
        public const string CircuitBreakerOpen = "CIRCUIT_BREAKER_OPEN";
        public const string TokenLimitExceeded = "TOKEN_LIMIT_EXCEEDED";
        public const string ProviderError = "PROVIDER_ERROR";
        public const string InternalError = "INTERNAL_ERROR";
    }

    /// <summary>
    /// HTTP header names
    /// </summary>
    public static class Headers
    {
        public const string ApiKey = "X-API-Key";
        public const string RequestId = "X-Request-ID";
        public const string UserId = "X-User-ID";
        public const string ClientVersion = "X-Client-Version";
        public const string RateLimitRemaining = "X-RateLimit-Remaining";
        public const string RateLimitReset = "X-RateLimit-Reset";
        public const string RateLimitLimit = "X-RateLimit-Limit";
        public const string ContentType = "Content-Type";
        public const string Authorization = "Authorization";
        public const string UserAgent = "User-Agent";
    }

    /// <summary>
    /// Content types
    /// </summary>
    public static class ContentTypes
    {
        public const string ApplicationJson = "application/json";
        public const string TextEventStream = "text/event-stream";
        public const string TextPlain = "text/plain";
        public const string ApplicationOctetStream = "application/octet-stream";
    }

    /// <summary>
    /// Provider names
    /// </summary>
    public static class Providers
    {
        public const string OpenAI = "OpenAI";
        public const string Anthropic = "Anthropic";
        public const string AzureOpenAI = "AzureOpenAI";
        public const string Cohere = "Cohere";
        public const string HuggingFace = "HuggingFace";
        public const string Google = "Google";
        public const string AWS = "AWS";
    }

    /// <summary>
    /// Model categories
    /// </summary>
    public static class ModelCategories
    {
        public const string TextCompletion = "text-completion";
        public const string ChatCompletion = "chat-completion";
        public const string Embedding = "embedding";
        public const string ImageGeneration = "image-generation";
        public const string CodeGeneration = "code-generation";
        public const string Translation = "translation";
        public const string Summarization = "summarization";
    }

    /// <summary>
    /// Message roles
    /// </summary>
    public static class MessageRoles
    {
        public const string System = "system";
        public const string User = "user";
        public const string Assistant = "assistant";
        public const string Function = "function";
        public const string Tool = "tool";
    }

    /// <summary>
    /// Circuit breaker states
    /// </summary>
    public static class CircuitBreakerStates
    {
        public const string Closed = "Closed";
        public const string Open = "Open";
        public const string HalfOpen = "HalfOpen";
    }

    /// <summary>
    /// Content filtering categories
    /// </summary>
    public static class ContentFilterCategories
    {
        public const string Hate = "hate";
        public const string Violence = "violence";
        public const string SelfHarm = "self-harm";
        public const string Sexual = "sexual";
        public const string Harassment = "harassment";
        public const string IllegalActivity = "illegal-activity";
        public const string Spam = "spam";
        public const string PersonalInformation = "personal-information";
    }

    /// <summary>
    /// Metrics names
    /// </summary>
    public static class Metrics
    {
        public const string CompletionsTotal = "llm_completions_total";
        public const string EmbeddingsTotal = "llm_embeddings_total";
        public const string CacheAccessTotal = "cache_access_total";
        public const string RateLimitTotal = "rate_limit_total";
        public const string CircuitBreakerStateChanges = "circuit_breaker_state_changes_total";
        public const string ContentFilterTotal = "content_filter_total";
        public const string TokenUsageTotal = "token_usage_total";
        public const string ProviderHealthChecks = "provider_health_checks_total";
        public const string CompletionDuration = "llm_completion_duration_ms";
        public const string EmbeddingDuration = "llm_embedding_duration_ms";
        public const string ProviderResponseTime = "provider_response_time_ms";
        public const string TokenCount = "token_count";
        public const string RequestCost = "request_cost_usd";
    }

    /// <summary>
    /// Configuration section names
    /// </summary>
    public static class ConfigurationSections
    {
        public const string GlobalOptions = "GlobalOptions";
        public const string Providers = "Providers";
        public const string RateLimit = "RateLimit";
        public const string ContentFiltering = "ContentFiltering";
        public const string Telemetry = "Telemetry";
        public const string OpenTelemetry = "OpenTelemetry";
        public const string Caching = "Caching";
        public const string TokenUsage = "TokenUsage";
        public const string Routing = "Routing";
        public const string Security = "Security";
    }

    /// <summary>
    /// Environment variable names
    /// </summary>
    public static class EnvironmentVariables
    {
        public const string AspNetCoreEnvironment = "ASPNETCORE_ENVIRONMENT";
        public const string OpenAIApiKey = "OPENAI_API_KEY";
        public const string AnthropicApiKey = "ANTHROPIC_API_KEY";
        public const string CohereApiKey = "COHERE_API_KEY";
        public const string HuggingFaceApiKey = "HUGGINGFACE_API_KEY";
        public const string AzureOpenAIApiKey = "AZURE_OPENAI_API_KEY";
        public const string AzureOpenAIEndpoint = "AZURE_OPENAI_ENDPOINT";
        public const string RedisConnectionString = "REDIS_CONNECTION_STRING";
        public const string DatabaseConnectionString = "DATABASE_CONNECTION_STRING";
    }

    /// <summary>
    /// File extensions
    /// </summary>
    public static class FileExtensions
    {
        public const string Json = ".json";
        public const string Yaml = ".yaml";
        public const string Yml = ".yml";
        public const string Txt = ".txt";
        public const string Log = ".log";
    }

    /// <summary>
    /// Regular expression patterns
    /// </summary>
    public static class RegexPatterns
    {
        public const string ApiKeyPattern = @"^[a-zA-Z0-9\-_]{32,}$";
        public const string ModelIdPattern = @"^[a-zA-Z0-9\-_.]+$";
        public const string ProviderNamePattern = @"^[a-zA-Z0-9\-_]+$";
        public const string EmailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        public const string UuidPattern = @"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$";
    }

    /// <summary>
    /// Time spans
    /// </summary>
    public static class TimeSpans
    {
        public static readonly TimeSpan DefaultCacheExpiration = TimeSpan.FromMinutes(60);
        public static readonly TimeSpan ShortCacheExpiration = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan LongCacheExpiration = TimeSpan.FromHours(24);
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
        public static readonly TimeSpan LongTimeout = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan CircuitBreakerTimeout = TimeSpan.FromMinutes(1);
        public static readonly TimeSpan HealthCheckInterval = TimeSpan.FromMinutes(1);
        public static readonly TimeSpan MetricsFlushInterval = TimeSpan.FromSeconds(30);
    }
}
