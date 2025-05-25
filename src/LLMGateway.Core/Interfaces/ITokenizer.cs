namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Interface for tokenizing text for different models
/// </summary>
public interface ITokenizer
{
    /// <summary>
    /// Count the number of tokens in the given text
    /// </summary>
    /// <param name="text">Text to tokenize</param>
    /// <returns>Number of tokens</returns>
    int CountTokens(string text);

    /// <summary>
    /// Encode text into tokens
    /// </summary>
    /// <param name="text">Text to encode</param>
    /// <returns>Array of token IDs</returns>
    int[] Encode(string text);

    /// <summary>
    /// Decode tokens back to text
    /// </summary>
    /// <param name="tokens">Token IDs to decode</param>
    /// <returns>Decoded text</returns>
    string Decode(int[] tokens);

    /// <summary>
    /// Get the model name this tokenizer is designed for
    /// </summary>
    string ModelName { get; }

    /// <summary>
    /// Get the maximum context length for this model
    /// </summary>
    int MaxContextLength { get; }
}
