using LLMGateway.API.Controllers.Base;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Completion;
using LLMGateway.Core.Models.Conversation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LLMGateway.API.Controllers;

/// <summary>
/// Controller for conversations
/// </summary>
[ApiVersion("1.0")]
[Authorize]
public class ConversationsController : BaseApiController
{
    private readonly IConversationService _conversationService;
    private readonly ILogger<ConversationsController> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="conversationService">Conversation service</param>
    /// <param name="logger">Logger</param>
    public ConversationsController(
        IConversationService conversationService,
        ILogger<ConversationsController> logger)
    {
        _conversationService = conversationService;
        _logger = logger;
    }

    /// <summary>
    /// Get the current user ID from the claims
    /// </summary>
    /// <returns>User ID</returns>
    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
    }

    /// <summary>
    /// Get all conversations
    /// </summary>
    /// <param name="includeArchived">Whether to include archived conversations</param>
    /// <returns>List of conversations</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Conversation>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Conversation>>> GetAllConversationsAsync([FromQuery] bool includeArchived = false)
    {
        var userId = GetUserId();
        var conversations = await _conversationService.GetAllConversationsAsync(userId, includeArchived);
        return Ok(conversations);
    }

    /// <summary>
    /// Get conversation by ID
    /// </summary>
    /// <param name="id">Conversation ID</param>
    /// <returns>Conversation</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Conversation), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Conversation>> GetConversationAsync(string id)
    {
        var userId = GetUserId();
        var conversation = await _conversationService.GetConversationAsync(id, userId);
        return Ok(conversation);
    }

    /// <summary>
    /// Create conversation
    /// </summary>
    /// <param name="request">Create request</param>
    /// <returns>Created conversation</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Conversation), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Conversation>> CreateConversationAsync(CreateConversationRequest request)
    {
        var userId = GetUserId();
        var conversation = await _conversationService.CreateConversationAsync(request, userId);
        return CreatedAtAction(nameof(GetConversationAsync), new { id = conversation.Id }, conversation);
    }

    /// <summary>
    /// Update conversation
    /// </summary>
    /// <param name="id">Conversation ID</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated conversation</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Conversation), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Conversation>> UpdateConversationAsync(string id, UpdateConversationRequest request)
    {
        var userId = GetUserId();
        var conversation = await _conversationService.UpdateConversationAsync(id, request, userId);
        return Ok(conversation);
    }

    /// <summary>
    /// Delete conversation
    /// </summary>
    /// <param name="id">Conversation ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteConversationAsync(string id)
    {
        var userId = GetUserId();
        await _conversationService.DeleteConversationAsync(id, userId);
        return NoContent();
    }

    /// <summary>
    /// Get conversation messages
    /// </summary>
    /// <param name="id">Conversation ID</param>
    /// <returns>List of messages</returns>
    [HttpGet("{id}/messages")]
    [ProducesResponseType(typeof(IEnumerable<ConversationMessage>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<ConversationMessage>>> GetConversationMessagesAsync(string id)
    {
        var userId = GetUserId();
        var messages = await _conversationService.GetConversationMessagesAsync(id, userId);
        return Ok(messages);
    }

    /// <summary>
    /// Add message to conversation
    /// </summary>
    /// <param name="id">Conversation ID</param>
    /// <param name="request">Add message request</param>
    /// <returns>Added message</returns>
    [HttpPost("{id}/messages")]
    [ProducesResponseType(typeof(ConversationMessage), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ConversationMessage>> AddMessageAsync(string id, AddMessageRequest request)
    {
        var userId = GetUserId();
        var message = await _conversationService.AddMessageAsync(id, request, userId);
        return CreatedAtAction(nameof(GetMessageAsync), new { id = message.Id }, message);
    }

    /// <summary>
    /// Get message by ID
    /// </summary>
    /// <param name="id">Message ID</param>
    /// <returns>Message</returns>
    [HttpGet("messages/{id}")]
    [ProducesResponseType(typeof(ConversationMessage), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ConversationMessage>> GetMessageAsync(string id)
    {
        var userId = GetUserId();
        var message = await _conversationService.GetMessageAsync(id, userId);
        return Ok(message);
    }

    /// <summary>
    /// Delete message
    /// </summary>
    /// <param name="id">Message ID</param>
    /// <returns>No content</returns>
    [HttpDelete("messages/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteMessageAsync(string id)
    {
        var userId = GetUserId();
        await _conversationService.DeleteMessageAsync(id, userId);
        return NoContent();
    }

    /// <summary>
    /// Continue conversation
    /// </summary>
    /// <param name="request">Continue conversation request</param>
    /// <returns>Completion response</returns>
    [HttpPost("continue")]
    [ProducesResponseType(typeof(CompletionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CompletionResponse>> ContinueConversationAsync(ContinueConversationRequest request)
    {
        var userId = GetUserId();
        var response = await _conversationService.ContinueConversationAsync(request, userId);
        return Ok(response);
    }

    /// <summary>
    /// Search conversations
    /// </summary>
    /// <param name="request">Search request</param>
    /// <returns>Search response</returns>
    [HttpPost("search")]
    [ProducesResponseType(typeof(ConversationSearchResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ConversationSearchResponse>> SearchConversationsAsync(ConversationSearchRequest request)
    {
        var userId = GetUserId();
        var response = await _conversationService.SearchConversationsAsync(request, userId);
        return Ok(response);
    }
}
