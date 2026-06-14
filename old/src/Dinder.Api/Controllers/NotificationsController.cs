using System.Security.Claims;
using Dinder.Application.Notifications.Commands;
using Dinder.Application.Notifications.Queries;
using Dinder.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dinder.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Get cursor-paginated notifications for the current user.</summary>
    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] Guid? cursor = null,
        [FromQuery] int limit = 20)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        var result = await _mediator.Send(new GetNotificationsQuery(userId.Value, cursor, limit));
        return Ok(result);
    }

    /// <summary>Register or reassign a device token for push notifications.</summary>
    [HttpPost("register-token")]
    public async Task<IActionResult> RegisterToken([FromBody] RegisterDeviceTokenRequest request)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        try
        {
            await _mediator.Send(new RegisterDeviceTokenCommand(userId.Value, request.Token, request.Platform));
            return NoContent();
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
    }

    /// <summary>Mark notifications as read (single or batch).</summary>
    [HttpPost("read")]
    public async Task<IActionResult> MarkRead([FromBody] MarkReadRequest request)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        var markedCount = await _mediator.Send(new MarkNotificationsReadCommand(userId.Value, request.NotificationIds));
        return Ok(new { markedCount });
    }

    /// <summary>Update per-type notification opt-out preference.</summary>
    [HttpPut("opt-out")]
    public async Task<IActionResult> UpdateOptOut([FromBody] UpdateOptOutRequest request)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        await _mediator.Send(new UpdateOptOutCommand(userId.Value, request.Type, request.OptOut));
        return NoContent();
    }

    private Guid? GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

// ── Request DTOs ──────────────────────────────────────────────────────

public sealed record RegisterDeviceTokenRequest(string Token, DevicePlatform Platform);
public sealed record MarkReadRequest(List<Guid>? NotificationIds = null);
public sealed record UpdateOptOutRequest(NotificationType Type, bool OptOut);
