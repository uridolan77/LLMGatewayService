using System;
using System.Linq;
using System.Text.RegularExpressions;
using LLMGateway.Core.Interfaces;

namespace LLMGateway.Core.Services.Tokenizers;

/// <summary>
/// Base tokenizer implementation with common functionality
/// </summary>
public abstract class BaseTokenizer : ITokenizer
{
    protected readonly Regex WordBoundaryRegex = new(@"\b\w+\b|\W+", RegexOptions.Compiled);
    protected readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    /// <inheritdoc/>
    public abstract string ModelName { get; }

    /// <inheritdoc/>
    public abstract int MaxContextLength { get; }

    /// <inheritdoc/>
    public virtual int CountTokens(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        // Basic tokenization strategy - can be overridden by specific implementations
        return EstimateTokenCount(text);
    }

    /// <inheritdoc/>
    public virtual int[] Encode(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<int>();
        }

        // Simple encoding - split by word boundaries and assign sequential IDs
        var words = WordBoundaryRegex.Matches(text)
            .Cast<Match>()
            .Select(m => m.Value)
            .ToArray();

        return words.Select((word, index) => word.GetHashCode() % 50000).ToArray();
    }

    /// <inheritdoc/>
    public virtual string Decode(int[] tokens)
    {
        // This is a simplified implementation - real tokenizers would have proper vocabulary
        return string.Join("", tokens.Select(t => $"[{t}]"));
    }

    /// <summary>
    /// Estimate token count using heuristics
    /// </summary>
    /// <param name="text">Text to estimate</param>
    /// <returns>Estimated token count</returns>
    protected virtual int EstimateTokenCount(string text)
    {
        // Different models have different tokenization characteristics
        var characterCount = text.Length;
        var wordCount = WordBoundaryRegex.Matches(text).Count;
        
        // Use a combination of character and word count for estimation
        // This is a heuristic and can be improved with model-specific data
        return Math.Max(1, (int)Math.Ceiling(characterCount / GetCharactersPerToken()) + 
                          (int)Math.Ceiling(wordCount * GetWordTokenMultiplier()));
    }

    /// <summary>
    /// Get the average number of characters per token for this model
    /// </summary>
    /// <returns>Characters per token</returns>
    protected abstract double GetCharactersPerToken();

    /// <summary>
    /// Get the multiplier for word-based token estimation
    /// </summary>
    /// <returns>Word token multiplier</returns>
    protected virtual double GetWordTokenMultiplier()
    {
        return 0.1; // Small adjustment for word boundaries
    }
}

/// <summary>
/// Tokenizer for GPT-4 models
/// </summary>
public class GPT4Tokenizer : BaseTokenizer
{
    public override string ModelName => "GPT-4";
    public override int MaxContextLength => 8192;

    protected override double GetCharactersPerToken() => 4.0; // GPT-4 averages about 4 characters per token
}

/// <summary>
/// Tokenizer for GPT-3.5 models
/// </summary>
public class GPT35Tokenizer : BaseTokenizer
{
    public override string ModelName => "GPT-3.5";
    public override int MaxContextLength => 4096;

    protected override double GetCharactersPerToken() => 4.0; // Similar to GPT-4
}

/// <summary>
/// Tokenizer for Claude models
/// </summary>
public class ClaudeTokenizer : BaseTokenizer
{
    public override string ModelName => "Claude";
    public override int MaxContextLength => 100000;

    protected override double GetCharactersPerToken() => 3.5; // Claude tends to be slightly more efficient
}

/// <summary>
/// Tokenizer for LLaMA models
/// </summary>
public class LlamaTokenizer : BaseTokenizer
{
    public override string ModelName => "LLaMA";
    public override int MaxContextLength => 2048;

    protected override double GetCharactersPerToken() => 4.5; // LLaMA uses a different tokenization scheme
}

/// <summary>
/// Default tokenizer for unknown models
/// </summary>
public class DefaultTokenizer : BaseTokenizer
{
    public override string ModelName => "Default";
    public override int MaxContextLength => 4096;

    protected override double GetCharactersPerToken() => 4.0; // Conservative estimate
}
