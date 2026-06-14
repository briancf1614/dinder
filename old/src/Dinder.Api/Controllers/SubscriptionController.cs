using System.Security.Claims;
using Dinder.Application.Subscription.Commands;
using Dinder.Application.Subscription.Queries;
using Dinder.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dinder.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public sealed class SubscriptionController : ControllerBase
{
    private readonly IMediator _mediator;

    public SubscriptionController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Initiate a Stripe Checkout session for a subscription tier.</summary>
    [HttpPost("checkout")]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] CheckoutRequest request)
    {
        var userId = GetUserId();
        var email = GetUserEmail();

        if (userId is null || email is null)
            return Unauthorized();

        try
        {
            var result = await _mediator.Send(new CreateCheckoutSessionCommand(
                userId.Value, email, request.Tier));
            return Ok(new { sessionUrl = result.SessionUrl });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>Get the current user's subscription status.</summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        var result = await _mediator.Send(new GetSubscriptionStatusQuery(userId.Value));
        if (result is null)
            return NotFound(new { error = "User not found." });

        return Ok(result);
    }

    /// <summary>Open the Stripe Customer Portal for managing billing.</summary>
    [HttpPost("portal")]
    public async Task<IActionResult> CreatePortalSession()
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        try
        {
            var result = await _mediator.Send(new CreatePortalSessionCommand(userId.Value));
            return Ok(new { portalUrl = result.PortalUrl });
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

    private string? GetUserEmail()
    {
        return User.FindFirstValue(ClaimTypes.Email)
            ?? User.FindFirstValue("email");
    }
}

// ── Request DTOs ──────────────────────────────────────────────────────

public sealed record CheckoutRequest(SubscriptionTier Tier);
