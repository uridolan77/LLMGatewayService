using LLMGateway.Core.Models.ContentFiltering;

namespace LLMGateway.Core.Interfaces;

/// <summary>
/// Interface for content filtering service
/// </summary>
public interface IContentFilteringService
{
    /// <summary>
    /// Filter content
    /// </summary>
    /// <param name="content">Content to filter</param>
    /// <returns>Filter result</returns>
    Task<ContentFilterResult> FilterContentAsync(string content);
    
    /// <summary>
    /// Filter prompt
    /// </summary>
    /// <param name="prompt">Prompt to filter</param>
    /// <returns>Filter result</returns>
    Task<ContentFilterResult> FilterPromptAsync(string prompt);
    
    /// <summary>
    /// Filter completion
    /// </summary>
    /// <param name="completion">Completion to filter</param>
    /// <returns>Filter result</returns>
    Task<ContentFilterResult> FilterCompletionAsync(string completion);
}
