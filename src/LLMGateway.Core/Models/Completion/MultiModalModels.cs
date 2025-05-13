namespace LLMGateway.Core.Models.Completion;

/// <summary>
/// Content type for multi-modal content
/// </summary>
public enum ContentType
{
    /// <summary>
    /// Text content
    /// </summary>
    Text,

    /// <summary>
    /// Image content
    /// </summary>
    Image,

    /// <summary>
    /// Audio content
    /// </summary>
    Audio,

    /// <summary>
    /// Video content
    /// </summary>
    Video
}

/// <summary>
/// Content part for multi-modal messages
/// </summary>
public class ContentPart
{
    /// <summary>
    /// Content type
    /// </summary>
    public ContentType Type { get; set; } = ContentType.Text;

    /// <summary>
    /// Text content (for text type)
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Image URL (for image type)
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Image data (for image type, base64 encoded)
    /// </summary>
    public string? ImageData { get; set; }

    /// <summary>
    /// Image detail level (for image type)
    /// </summary>
    public ImageDetail? Detail { get; set; }

    /// <summary>
    /// Audio URL (for audio type)
    /// </summary>
    public string? AudioUrl { get; set; }

    /// <summary>
    /// Audio data (for audio type, base64 encoded)
    /// </summary>
    public string? AudioData { get; set; }

    /// <summary>
    /// Video URL (for video type)
    /// </summary>
    public string? VideoUrl { get; set; }

    /// <summary>
    /// Video data (for video type, base64 encoded)
    /// </summary>
    public string? VideoData { get; set; }
}

/// <summary>
/// Image detail level for vision models
/// </summary>
public enum ImageDetail
{
    /// <summary>
    /// Auto detail level (default)
    /// </summary>
    Auto,

    /// <summary>
    /// Low detail level
    /// </summary>
    Low,

    /// <summary>
    /// High detail level
    /// </summary>
    High
}

/// <summary>
/// Multi-modal message
/// </summary>
public class MultiModalMessage
{
    /// <summary>
    /// Message role
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Content parts
    /// </summary>
    public List<ContentPart> Content { get; set; } = new();
}

/// <summary>
/// Multi-modal completion request
/// </summary>
public class MultiModalCompletionRequest
{
    /// <summary>
    /// Model ID
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Messages
    /// </summary>
    public List<MultiModalMessage> Messages { get; set; } = new();

    /// <summary>
    /// Temperature
    /// </summary>
    public float? Temperature { get; set; }

    /// <summary>
    /// Top P
    /// </summary>
    public float? TopP { get; set; }

    /// <summary>
    /// Max tokens
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Stream
    /// </summary>
    public bool Stream { get; set; } = false;

    /// <summary>
    /// User
    /// </summary>
    public string? User { get; set; }
}

/// <summary>
/// Helper methods for multi-modal messages
/// </summary>
public static class MultiModalMessageExtensions
{
    /// <summary>
    /// Convert a standard message to a multi-modal message
    /// </summary>
    /// <param name="message">Standard message</param>
    /// <returns>Multi-modal message</returns>
    public static MultiModalMessage ToMultiModalMessage(this Message message)
    {
        return new MultiModalMessage
        {
            Role = message.Role,
            Content = new List<ContentPart>
            {
                new ContentPart
                {
                    Type = ContentType.Text,
                    Text = message.Content
                }
            }
        };
    }

    /// <summary>
    /// Convert a multi-modal message to a standard message
    /// </summary>
    /// <param name="message">Multi-modal message</param>
    /// <returns>Standard message</returns>
    public static Message ToStandardMessage(this MultiModalMessage message)
    {
        var textParts = message.Content
            .Where(c => c.Type == ContentType.Text && !string.IsNullOrEmpty(c.Text))
            .Select(c => c.Text)
            .ToList();

        var imageCount = message.Content.Count(c => c.Type == ContentType.Image);
        var audioCount = message.Content.Count(c => c.Type == ContentType.Audio);
        var videoCount = message.Content.Count(c => c.Type == ContentType.Video);

        var contentBuilder = new System.Text.StringBuilder();

        if (textParts.Any())
        {
            contentBuilder.AppendJoin("\n", textParts);
        }

        if (imageCount > 0 || audioCount > 0 || videoCount > 0)
        {
            if (contentBuilder.Length > 0)
            {
                contentBuilder.AppendLine();
            }

            if (imageCount > 0)
            {
                contentBuilder.AppendLine($"[{imageCount} image{(imageCount > 1 ? "s" : "")}]");
            }

            if (audioCount > 0)
            {
                contentBuilder.AppendLine($"[{audioCount} audio file{(audioCount > 1 ? "s" : "")}]");
            }

            if (videoCount > 0)
            {
                contentBuilder.AppendLine($"[{videoCount} video file{(videoCount > 1 ? "s" : "")}]");
            }
        }

        return new Message
        {
            Role = message.Role,
            Content = contentBuilder.ToString().Trim()
        };
    }

    /// <summary>
    /// Convert a standard completion request to a multi-modal completion request
    /// </summary>
    /// <param name="request">Standard completion request</param>
    /// <returns>Multi-modal completion request</returns>
    public static MultiModalCompletionRequest ToMultiModalRequest(this CompletionRequest request)
    {
        return new MultiModalCompletionRequest
        {
            ModelId = request.ModelId,
            Messages = request.Messages.Select(m => m.ToMultiModalMessage()).ToList(),
            Temperature = request.Temperature.HasValue ? (float)request.Temperature.Value : null,
            TopP = request.TopP.HasValue ? (float)request.TopP.Value : null,
            MaxTokens = request.MaxTokens,
            Stream = request.Stream,
            User = request.User
        };
    }
}
