using System.Security.Claims;
using Dinder.Application.Chat.Commands;
using Dinder.Application.Chat.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dinder.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public sealed class ChatController : ControllerBase
{
    private readonly IMediator _mediator;

    public ChatController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Get cursor-paginated list of the authenticated user's active conversations.</summary>
    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations(
        [FromQuery] Guid? cursor = null,
        [FromQuery] int limit = 20)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        var result = await _mediator.Send(new GetConversationsQuery(userId.Value, cursor, limit));
        return Ok(result);
    }

    /// <summary>Get cursor-paginated message history for a conversation.</summary>
    [HttpGet("conversations/{conversationId:guid}/messages")]
    public async Task<IActionResult> GetMessages(
        Guid conversationId,
        [FromQuery] Guid? cursor = null,
        [FromQuery] int limit = 50)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        try
        {
            var result = await _mediator.Send(new GetMessagesQuery(conversationId, userId.Value, cursor, limit));
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Unmatch a conversation. Hides from both users, retains messages for moderation.</summary>
    [HttpPost("conversations/{conversationId:guid}/unmatch")]
    public async Task<IActionResult> Unmatch(Guid conversationId)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        try
        {
            await _mediator.Send(new UnmatchCommand(conversationId, userId.Value));
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private Guid? GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
