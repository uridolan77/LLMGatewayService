using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.ContentFiltering;
using LLMGateway.Core.Models.Completion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LLMGateway.Core.Services;

/// <summary>
/// Service for filtering content
/// </summary>
public class ContentFilteringService : IContentFilteringService
{
    private readonly ILogger<ContentFilteringService> _logger;
    private readonly ContentFilteringOptions _options;
    private readonly List<Regex> _blockedRegexPatterns = new();

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger">Logger</param>
    /// <param name="options">Content filtering options</param>
    public ContentFilteringService(
        ILogger<ContentFilteringService> logger,
        IOptions<ContentFilteringOptions> options)
    {
        _logger = logger;
        _options = options.Value;

        // Compile regex patterns
        foreach (var pattern in _options.BlockedPatterns)
        {
            try
            {
                _blockedRegexPatterns.Add(new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Invalid regex pattern: {Pattern}", pattern);
            }
        }
    }

    /// <inheritdoc/>
    public async Task<ContentFilterResult> FilterContentAsync(string content)
    {
        if (!_options.EnableContentFiltering)
        {
            return ContentFilterResult.Allowed();
        }

        try
        {
            // Try to parse the content as a completion request
            var completionRequest = JsonSerializer.Deserialize<CompletionRequest>(content);
            if (completionRequest != null && completionRequest.Messages != null && completionRequest.Messages.Any())
            {
                // Filter each message
                foreach (var message in completionRequest.Messages)
                {
                    if (!string.IsNullOrEmpty(message.Content))
                    {
                        var result = await FilterPromptAsync(message.Content);
                        if (!result.IsAllowed)
                        {
                            return result;
                        }
                    }
                }

                return ContentFilterResult.Allowed();
            }
        }
        catch (JsonException)
        {
            // Not a completion request, continue with regular filtering
        }

        // Check for blocked terms
        foreach (var term in _options.BlockedTerms)
        {
            if (content.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                return ContentFilterResult.Filtered(
                    $"Content contains blocked term: {term}",
                    "blocked_term");
            }
        }

        // Check for blocked patterns
        foreach (var regex in _blockedRegexPatterns)
        {
            var match = regex.Match(content);
            if (match.Success)
            {
                return ContentFilterResult.Filtered(
                    $"Content matches blocked pattern: {regex}",
                    "blocked_pattern");
            }
        }

        // Perform content classification
        var classification = await ClassifyContentAsync(content);

        // Check thresholds
        var categories = new List<string>();
        var reasons = new List<string>();

        if (classification.TryGetValue("hate", out var hateScore) && hateScore >= _options.HateThreshold)
        {
            categories.Add("hate");
            reasons.Add($"Hate content detected (score: {hateScore:F2})");
        }

        if (classification.TryGetValue("harassment", out var harassmentScore) && harassmentScore >= _options.HarassmentThreshold)
        {
            categories.Add("harassment");
            reasons.Add($"Harassment content detected (score: {harassmentScore:F2})");
        }

        if (classification.TryGetValue("self_harm", out var selfHarmScore) && selfHarmScore >= _options.SelfHarmThreshold)
        {
            categories.Add("self_harm");
            reasons.Add($"Self-harm content detected (score: {selfHarmScore:F2})");
        }

        if (classification.TryGetValue("sexual", out var sexualScore) && sexualScore >= _options.SexualThreshold)
        {
            categories.Add("sexual");
            reasons.Add($"Sexual content detected (score: {sexualScore:F2})");
        }

        if (classification.TryGetValue("violence", out var violenceScore) && violenceScore >= _options.ViolenceThreshold)
        {
            categories.Add("violence");
            reasons.Add($"Violence content detected (score: {violenceScore:F2})");
        }

        if (categories.Count > 0)
        {
            return new ContentFilterResult
            {
                IsAllowed = false,
                Reason = string.Join("; ", reasons),
                Categories = categories,
                Scores = classification
            };
        }

        return new ContentFilterResult
        {
            IsAllowed = true,
            Scores = classification
        };
    }

    /// <inheritdoc/>
    public async Task<ContentFilterResult> FilterPromptAsync(string prompt)
    {
        if (!_options.EnableContentFiltering || !_options.FilterPrompts)
        {
            return ContentFilterResult.Allowed();
        }

        return await FilterContentAsync(prompt);
    }

    /// <inheritdoc/>
    public async Task<ContentFilterResult> FilterCompletionAsync(string completion)
    {
        if (!_options.EnableContentFiltering || !_options.FilterCompletions)
        {
            return ContentFilterResult.Allowed();
        }

        return await FilterContentAsync(completion);
    }

    private Task<Dictionary<string, double>> ClassifyContentAsync(string content)
    {
        // In a real implementation, this would call a content moderation API or use a local model
        // For this example, we'll return a simple classification based on keyword matching

        var result = new Dictionary<string, double>
        {
            ["hate"] = 0.0,
            ["harassment"] = 0.0,
            ["self_harm"] = 0.0,
            ["sexual"] = 0.0,
            ["violence"] = 0.0
        };

        // Simple keyword-based classification
        var lowerContent = content.ToLowerInvariant();

        // Hate speech keywords
        var hateKeywords = new[] { "hate", "racist", "bigot", "nazi", "supremacist" };
        result["hate"] = CalculateScore(lowerContent, hateKeywords);

        // Harassment keywords
        var harassmentKeywords = new[] { "harass", "bully", "stalk", "threaten", "intimidate" };
        result["harassment"] = CalculateScore(lowerContent, harassmentKeywords);

        // Self-harm keywords
        var selfHarmKeywords = new[] { "suicide", "self-harm", "kill myself", "hurt myself", "end my life" };
        result["self_harm"] = CalculateScore(lowerContent, selfHarmKeywords);

        // Sexual keywords
        var sexualKeywords = new[] { "porn", "explicit", "nude", "sexual", "xxx" };
        result["sexual"] = CalculateScore(lowerContent, sexualKeywords);

        // Violence keywords
        var violenceKeywords = new[] { "kill", "murder", "attack", "bomb", "weapon" };
        result["violence"] = CalculateScore(lowerContent, violenceKeywords);

        return Task.FromResult(result);
    }

    private static double CalculateScore(string content, string[] keywords)
    {
        double score = 0.0;

        foreach (var keyword in keywords)
        {
            if (content.Contains(keyword))
            {
                score += 0.2;
            }
        }

        return Math.Min(score, 1.0);
    }
}
