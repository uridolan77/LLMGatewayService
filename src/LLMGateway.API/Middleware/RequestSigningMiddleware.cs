using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace LLMGateway.API.Middleware;

/// <summary>
/// Middleware for validating HMAC request signatures
/// </summary>
public class RequestSigningMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestSigningMiddleware> _logger;
    private readonly RequestSigningOptions _options;

    public RequestSigningMiddleware(
        RequestDelegate next,
        ILogger<RequestSigningMiddleware> logger,
        IOptions<RequestSigningOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip signature validation for certain paths
        if (ShouldSkipValidation(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Only validate signatures if enabled
        if (!_options.EnableSignatureValidation)
        {
            await _next(context);
            return;
        }

        try
        {
            if (!await ValidateSignatureAsync(context))
            {
                _logger.LogWarning("Request signature validation failed for {Path} from {RemoteIpAddress}",
                    context.Request.Path, context.Connection.RemoteIpAddress);

                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid signature");
                return;
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during signature validation");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Signature validation error");
        }
    }

    private async Task<bool> ValidateSignatureAsync(HttpContext context)
    {
        // Get signature from headers
        if (!context.Request.Headers.TryGetValue(_options.SignatureHeaderName, out var signatureHeader))
        {
            _logger.LogDebug("No signature header found");
            return !_options.RequireSignature; // Allow if signature is not required
        }

        var providedSignature = signatureHeader.FirstOrDefault();
        if (string.IsNullOrEmpty(providedSignature))
        {
            _logger.LogDebug("Empty signature header");
            return false;
        }

        // Get timestamp from headers for replay attack prevention
        var timestamp = GetTimestamp(context);
        if (timestamp.HasValue && IsTimestampExpired(timestamp.Value))
        {
            _logger.LogWarning("Request timestamp is expired: {Timestamp}", timestamp);
            return false;
        }

        // Get the request payload
        var payload = await GetRequestPayloadAsync(context.Request);

        // Get the secret key for the API key
        var apiKey = GetApiKey(context);
        var secretKey = await GetSecretKeyAsync(apiKey);
        if (string.IsNullOrEmpty(secretKey))
        {
            _logger.LogWarning("No secret key found for API key");
            return false;
        }

        // Compute the expected signature
        var expectedSignature = ComputeHmacSignature(payload, secretKey, timestamp);

        // Compare signatures using constant-time comparison
        var isValid = SecureCompare(providedSignature, expectedSignature);

        if (!isValid)
        {
            _logger.LogWarning("Signature mismatch. Expected: {Expected}, Provided: {Provided}",
                expectedSignature, providedSignature);
        }

        return isValid;
    }

    private bool ShouldSkipValidation(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant();
        
        // Skip validation for health checks, swagger, and other public endpoints
        var skipPaths = new[]
        {
            "/health",
            "/swagger",
            "/api-docs",
            "/favicon.ico",
            "/robots.txt"
        };

        return skipPaths.Any(skipPath => pathValue?.StartsWith(skipPath) == true);
    }

    private DateTime? GetTimestamp(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(_options.TimestampHeaderName, out var timestampHeader))
        {
            var timestampString = timestampHeader.FirstOrDefault();
            if (long.TryParse(timestampString, out var unixTimestamp))
            {
                return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).DateTime;
            }
        }

        return null;
    }

    private bool IsTimestampExpired(DateTime timestamp)
    {
        var age = DateTime.UtcNow - timestamp;
        return Math.Abs(age.TotalMinutes) > _options.TimestampToleranceMinutes;
    }

    private async Task<string> GetRequestPayloadAsync(HttpRequest request)
    {
        // Enable buffering to allow multiple reads
        request.EnableBuffering();

        var body = string.Empty;
        if (request.ContentLength > 0)
        {
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            body = await reader.ReadToEndAsync();
            request.Body.Position = 0; // Reset position for next middleware
        }

        // Include relevant request data in the payload
        var payloadData = new
        {
            Method = request.Method,
            Path = request.Path.Value,
            QueryString = request.QueryString.Value,
            Body = body,
            Timestamp = GetTimestamp(request.HttpContext)?.ToString("O")
        };

        return System.Text.Json.JsonSerializer.Serialize(payloadData);
    }

    private string GetApiKey(HttpContext context)
    {
        // Try to get API key from various sources
        if (context.Request.Headers.TryGetValue("X-API-Key", out var apiKeyHeader))
        {
            return apiKeyHeader.FirstOrDefault() ?? string.Empty;
        }

        if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var auth = authHeader.FirstOrDefault();
            if (auth?.StartsWith("Bearer ") == true)
            {
                return auth[7..]; // Remove "Bearer " prefix
            }
        }

        return string.Empty;
    }

    private async Task<string?> GetSecretKeyAsync(string apiKey)
    {
        // In a real implementation, this would fetch from a secure store
        // For now, use a simple mapping or configuration
        
        if (string.IsNullOrEmpty(apiKey))
        {
            return null;
        }

        // This should be replaced with actual secret management
        // e.g., Azure Key Vault, AWS Secrets Manager, etc.
        return _options.DefaultSecretKey;
    }

    private string ComputeHmacSignature(string payload, string secretKey, DateTime? timestamp)
    {
        var dataToSign = payload;
        if (timestamp.HasValue)
        {
            dataToSign += timestamp.Value.ToString("O");
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToSign));
        return Convert.ToBase64String(hash);
    }

    private static bool SecureCompare(string a, string b)
    {
        if (a.Length != b.Length)
        {
            return false;
        }

        var result = 0;
        for (var i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }

        return result == 0;
    }
}

/// <summary>
/// Options for request signing middleware
/// </summary>
public class RequestSigningOptions
{
    /// <summary>
    /// Whether to enable signature validation
    /// </summary>
    public bool EnableSignatureValidation { get; set; } = false;

    /// <summary>
    /// Whether to require signatures for all requests
    /// </summary>
    public bool RequireSignature { get; set; } = false;

    /// <summary>
    /// Header name for the signature
    /// </summary>
    public string SignatureHeaderName { get; set; } = "X-Signature";

    /// <summary>
    /// Header name for the timestamp
    /// </summary>
    public string TimestampHeaderName { get; set; } = "X-Timestamp";

    /// <summary>
    /// Timestamp tolerance in minutes
    /// </summary>
    public int TimestampToleranceMinutes { get; set; } = 5;

    /// <summary>
    /// Default secret key (should be replaced with proper secret management)
    /// </summary>
    public string DefaultSecretKey { get; set; } = "default-secret-key";
}
