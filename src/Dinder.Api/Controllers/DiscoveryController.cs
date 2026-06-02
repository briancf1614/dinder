using System.Security.Claims;
using Dinder.Application.Discovery.Commands;
using Dinder.Application.Discovery.Queries;
using Dinder.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dinder.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public sealed class DiscoveryController : ControllerBase
{
    private readonly IMediator _mediator;

    public DiscoveryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Get candidate profiles for the current user.</summary>
    [HttpGet("candidates")]
    public async Task<IActionResult> GetCandidates(
        [FromQuery] double latitude = 0,
        [FromQuery] double longitude = 0,
        [FromQuery] Guid? cursor = null,
        [FromQuery] int limit = 20)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        try
        {
            var result = await _mediator.Send(new GetCandidatesQuery(
                userId.Value, latitude, longitude, cursor, limit));
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Swipe on a candidate profile.</summary>
    [HttpPost("swipe")]
    public async Task<IActionResult> Swipe([FromBody] SwipeRequest request)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        try
        {
            var result = await _mediator.Send(new SwipeCommand(userId.Value, request.SwipedId, request.Direction));
            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("SWIPE_LIMIT_REACHED"))
        {
            var parts = ex.Message.Split(':');
            var resetAt = parts.Length > 1 ? parts[1] : null;
            var upgradeTier = parts.Length > 2 ? parts[2] : null;
            return StatusCode(429, new
            {
                error = "Daily swipe limit reached. Upgrade to continue swiping.",
                resetAt,
                upgrade_url = upgradeTier is not null
                    ? $"/api/v1/subscription/checkout"
                    : null,
                upgrade_tier = upgradeTier
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
    }

    /// <summary>Get active matches for the current user.</summary>
    [HttpGet("matches")]
    public async Task<IActionResult> GetMatches()
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        var result = await _mediator.Send(new GetMatchesQuery(userId.Value));
        return Ok(result);
    }

    private Guid? GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

// ── Request DTOs ──────────────────────────────────────────────────────

public sealed record SwipeRequest(Guid SwipedId, SwipeDirection Direction);
