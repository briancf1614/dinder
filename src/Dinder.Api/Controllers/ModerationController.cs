using System.Security.Claims;
using Dinder.Application.Moderation.Commands;
using Dinder.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dinder.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public sealed class ModerationController : ControllerBase
{
    private readonly IMediator _mediator;

    public ModerationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Report a user with required reason enum.</summary>
    [HttpPost("report")]
    public async Task<IActionResult> ReportUser([FromBody] ReportUserRequest request)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        try
        {
            var result = await _mediator.Send(new ReportUserCommand(
                userId.Value,
                request.ReportedUserId,
                request.Reason,
                request.SubCategory,
                request.Description));

            return Ok(new
            {
                reportId = result.ReportId,
                isDuplicate = result.IsDuplicate
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

    /// <summary>Block a user. One-way, immediate, no notification to blocked user.</summary>
    [HttpPost("block/{userId:guid}")]
    public async Task<IActionResult> BlockUser(Guid userId)
    {
        var blockerId = GetUserId();
        if (blockerId is null)
            return Unauthorized();

        try
        {
            var result = await _mediator.Send(new BlockUserCommand(blockerId.Value, userId));
            return Ok(new
            {
                blockId = result.BlockId,
                alreadyBlocked = result.AlreadyBlocked
            });
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

// ── Request DTOs ──────────────────────────────────────────────────────

public sealed record ReportUserRequest(Guid ReportedUserId, ReportReason Reason, string? SubCategory, string? Description);
