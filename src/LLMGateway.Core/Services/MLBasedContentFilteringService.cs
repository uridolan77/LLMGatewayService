using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Completion;
using LLMGateway.Core.Models.ContentFiltering;
using LLMGateway.Core.Options;
using LLMGateway.Core.Constants;

namespace LLMGateway.Core.Services;

/// <summary>
/// Enhanced content filtering service with ML-based filtering capabilities
/// </summary>
public class MLBasedContentFilteringService : IContentFilteringService
{
    private readonly ILogger<MLBasedContentFilteringService> _logger;
    private readonly IOptions<ContentFilteringOptions> _options;
    private readonly ICompletionService _completionService;
    private readonly IMetricsService _metricsService;
    private readonly List<Regex> _blockedRegexPatterns;

    public MLBasedContentFilteringService(
        ILogger<MLBasedContentFilteringService> logger,
        IOptions<ContentFilteringOptions> options,
        ICompletionService completionService,
        IMetricsService metricsService)
    {
        _logger = logger;
        _options = options;
        _completionService = completionService;
        _metricsService = metricsService;

        // Compile regex patterns for better performance
        _blockedRegexPatterns = _options.Value.BlockedPatterns?
            .Select(pattern => new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled))
            .ToList() ?? new List<Regex>();
    }

    /// <inheritdoc/>
    public async Task<ContentFilterResult> FilterContentAsync(string content)
    {
        if (!_options.Value.EnableContentFiltering)
        {
            return ContentFilterResult.Allowed();
        }

        try
        {
            // Try to parse the content as a completion request
            var completionRequest = JsonSerializer.Deserialize<CompletionRequest>(content);
            if (completionRequest?.Messages != null && completionRequest.Messages.Any())
            {
                // Filter each message
                foreach (var message in completionRequest.Messages)
                {
                    if (!string.IsNullOrEmpty(message.Content))
                    {
                        var result = await FilterTextAsync(message.Content, "prompt");
                        if (!result.IsAllowed)
                        {
                            _metricsService.RecordContentFiltering("prompt", true, result.Reason);
                            return result;
                        }
                    }
                }

                _metricsService.RecordContentFiltering("prompt", false);
                return ContentFilterResult.Allowed();
            }
        }
        catch (JsonException)
        {
            // Not a completion request, continue with regular filtering
        }

        var filterResult = await FilterTextAsync(content, "general");
        _metricsService.RecordContentFiltering("general", !filterResult.IsAllowed, filterResult.Reason);
        return filterResult;
    }

    /// <inheritdoc/>
    public async Task<ContentFilterResult> FilterPromptAsync(string prompt)
    {
        if (!_options.Value.EnableContentFiltering || !_options.Value.FilterPrompts)
        {
            return ContentFilterResult.Allowed();
        }

        var result = await FilterTextAsync(prompt, "prompt");
        _metricsService.RecordContentFiltering("prompt", !result.IsAllowed, result.Reason);
        return result;
    }

    /// <inheritdoc/>
    public async Task<ContentFilterResult> FilterCompletionAsync(string completion)
    {
        if (!_options.Value.EnableContentFiltering || !_options.Value.FilterCompletions)
        {
            return ContentFilterResult.Allowed();
        }

        var result = await FilterTextAsync(completion, "completion");
        _metricsService.RecordContentFiltering("completion", !result.IsAllowed, result.Reason);
        return result;
    }

    private async Task<ContentFilterResult> FilterTextAsync(string text, string contentType)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return ContentFilterResult.Allowed();
        }

        // 1. Basic keyword filtering
        var keywordResult = FilterByKeywords(text);
        if (!keywordResult.IsAllowed)
        {
            return keywordResult;
        }

        // 2. Regex pattern filtering
        var patternResult = FilterByPatterns(text);
        if (!patternResult.IsAllowed)
        {
            return patternResult;
        }

        // 3. ML-based filtering using a moderation model
        if (_options.Value.UseMLFiltering)
        {
            var mlResult = await FilterWithMLAsync(text, contentType);
            if (!mlResult.IsAllowed)
            {
                return mlResult;
            }
        }

        // 4. Custom rule-based filtering
        var customResult = ApplyCustomRules(text, contentType);
        if (!customResult.IsAllowed)
        {
            return customResult;
        }

        return ContentFilterResult.Allowed();
    }

    private ContentFilterResult FilterByKeywords(string text)
    {
        var lowerText = text.ToLowerInvariant();

        foreach (var term in _options.Value.BlockedTerms)
        {
            if (lowerText.Contains(term.ToLowerInvariant()))
            {
                _logger.LogWarning("Content blocked due to keyword: {Term}", term);
                return ContentFilterResult.Filtered(
                    $"Content contains blocked term: {term}",
                    LLMGatewayConstants.ContentFilterCategories.Harassment);
            }
        }

        return ContentFilterResult.Allowed();
    }

    private ContentFilterResult FilterByPatterns(string text)
    {
        foreach (var regex in _blockedRegexPatterns)
        {
            var match = regex.Match(text);
            if (match.Success)
            {
                _logger.LogWarning("Content blocked due to pattern match: {Pattern}", regex.ToString());
                return ContentFilterResult.Filtered(
                    $"Content matches blocked pattern",
                    LLMGatewayConstants.ContentFilterCategories.Harassment);
            }
        }

        return ContentFilterResult.Allowed();
    }

    private async Task<ContentFilterResult> FilterWithMLAsync(string text, string contentType)
    {
        try
        {
            // Use a dedicated moderation model for content classification
            var moderationRequest = new CompletionRequest
            {
                ModelId = _options.Value.ModerationModelId ?? "gpt-3.5-turbo",
                Messages = new List<Message>
                {
                    new()
                    {
                        Role = LLMGatewayConstants.MessageRoles.System,
                        Content = GetModerationSystemPrompt(contentType)
                    },
                    new()
                    {
                        Role = LLMGatewayConstants.MessageRoles.User,
                        Content = text
                    }
                },
                MaxTokens = 100,
                Temperature = 0.0 // Use deterministic responses for moderation
            };

            var response = await _completionService.CreateCompletionAsync(moderationRequest, CancellationToken.None);
            return ParseModerationResponse(response, text);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ML-based content filtering");

            // Fail open - allow content if moderation service fails
            if (_options.Value.FailOpenOnModerationError)
            {
                return ContentFilterResult.Allowed();
            }

            // Fail closed - block content if moderation service fails
            return ContentFilterResult.Filtered(
                "Content filtering service unavailable",
                LLMGatewayConstants.ContentFilterCategories.Harassment);
        }
    }

    private ContentFilterResult ApplyCustomRules(string text, string contentType)
    {
        // Custom rule: Check for potential personal information
        if (ContainsPotentialPII(text))
        {
            return ContentFilterResult.Filtered(
                "Content may contain personal information",
                LLMGatewayConstants.ContentFilterCategories.PersonalInformation);
        }

        // Custom rule: Check for potential spam patterns
        if (IsLikelySpam(text))
        {
            return ContentFilterResult.Filtered(
                "Content appears to be spam",
                LLMGatewayConstants.ContentFilterCategories.Spam);
        }

        // Custom rule: Check for excessive repetition
        if (HasExcessiveRepetition(text))
        {
            return ContentFilterResult.Filtered(
                "Content contains excessive repetition",
                LLMGatewayConstants.ContentFilterCategories.Spam);
        }

        return ContentFilterResult.Allowed();
    }

    private string GetModerationSystemPrompt(string contentType)
    {
        return contentType switch
        {
            "prompt" => @"You are a content moderation system. Analyze the following user prompt and classify it for safety.
                         Respond with a JSON object containing:
                         - ""safe"": boolean (true if content is safe, false if it should be blocked)
                         - ""categories"": array of strings (categories that apply: hate, violence, self-harm, sexual, harassment, illegal-activity, spam, personal-information)
                         - ""confidence"": number between 0 and 1
                         - ""reason"": string (brief explanation if not safe)

                         Be conservative but not overly restrictive. Allow legitimate educational, creative, and informational content.",

            "completion" => @"You are a content moderation system. Analyze the following AI-generated completion and classify it for safety.
                           Respond with a JSON object containing:
                           - ""safe"": boolean (true if content is safe, false if it should be blocked)
                           - ""categories"": array of strings (categories that apply: hate, violence, self-harm, sexual, harassment, illegal-activity, spam, personal-information)
                           - ""confidence"": number between 0 and 1
                           - ""reason"": string (brief explanation if not safe)

                           Focus on preventing harmful outputs while allowing helpful and informative responses.",

            _ => @"You are a content moderation system. Analyze the following content and classify it for safety.
                  Respond with a JSON object containing:
                  - ""safe"": boolean (true if content is safe, false if it should be blocked)
                  - ""categories"": array of strings (categories that apply: hate, violence, self-harm, sexual, harassment, illegal-activity, spam, personal-information)
                  - ""confidence"": number between 0 and 1
                  - ""reason"": string (brief explanation if not safe)"
        };
    }

    private ContentFilterResult ParseModerationResponse(CompletionResponse response, string originalText)
    {
        try
        {
            var content = response.Choices?.FirstOrDefault()?.Message?.Content;
            if (string.IsNullOrEmpty(content))
            {
                return ContentFilterResult.Allowed();
            }

            var moderation = JsonSerializer.Deserialize<ModerationResult>(content);
            if (moderation == null)
            {
                return ContentFilterResult.Allowed();
            }

            if (!moderation.Safe)
            {
                return new ContentFilterResult
                {
                    IsAllowed = false,
                    Reason = moderation.Reason ?? "Content flagged by moderation system",
                    Categories = moderation.Categories ?? new List<string>(),
                    Scores = new Dictionary<string, double> { { "confidence", moderation.Confidence } }
                };
            }

            return ContentFilterResult.Allowed();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse moderation response");
            return ContentFilterResult.Allowed(); // Fail open
        }
    }

    private bool ContainsPotentialPII(string text)
    {
        // Simple patterns for common PII - in production, use more sophisticated detection
        var piiPatterns = new[]
        {
            @"\b\d{3}-\d{2}-\d{4}\b", // SSN pattern
            @"\b\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}\b", // Credit card pattern
            @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", // Email pattern
            @"\b\d{3}[\s.-]?\d{3}[\s.-]?\d{4}\b" // Phone number pattern
        };

        return piiPatterns.Any(pattern => Regex.IsMatch(text, pattern));
    }

    private bool IsLikelySpam(string text)
    {
        // Simple spam detection heuristics
        var spamIndicators = new[]
        {
            "click here", "buy now", "limited time", "act now", "free money",
            "guaranteed", "no risk", "100% free", "make money fast"
        };

        var lowerText = text.ToLowerInvariant();
        var indicatorCount = spamIndicators.Count(indicator => lowerText.Contains(indicator));

        return indicatorCount >= 2; // Multiple spam indicators
    }

    private bool HasExcessiveRepetition(string text)
    {
        if (text.Length < 50) return false;

        // Check for repeated words
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 10) return false;

        var wordCounts = words.GroupBy(w => w.ToLowerInvariant())
                             .ToDictionary(g => g.Key, g => g.Count());

        // If any word appears more than 30% of the time, consider it excessive
        var maxWordFrequency = wordCounts.Values.Max() / (double)words.Length;
        return maxWordFrequency > 0.3;
    }

    private class ModerationResult
    {
        public bool Safe { get; set; }
        public List<string>? Categories { get; set; }
        public double Confidence { get; set; }
        public string? Reason { get; set; }
    }
}
