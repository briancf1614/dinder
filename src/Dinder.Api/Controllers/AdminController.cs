using System.Security.Claims;
using Dinder.Application.Admin.Commands;
using Dinder.Application.Admin.Queries;
using Dinder.Application.Moderation.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dinder.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Admin")]
public sealed class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Search users by email (partial match) or ID (exact match).</summary>
    [HttpGet("users")]
    public async Task<IActionResult> SearchUsers(
        [FromQuery] string? q = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = "Query parameter 'q' is required." });

        var result = await _mediator.Send(new AdminGetUsersQuery(q, page, pageSize));
        return Ok(result);
    }

    /// <summary>Get reports queue, optionally filtered by status.</summary>
    [HttpGet("reports")]
    public async Task<IActionResult> GetReports(
        [FromQuery] string? status = null)
    {
        Domain.Enums.ReportStatus? reportStatus = null;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<Domain.Enums.ReportStatus>(status, true, out var parsed))
            reportStatus = parsed;

        var result = await _mediator.Send(new GetReportsQuery(reportStatus));
        return Ok(result);
    }

    /// <summary>Resolve or dismiss a report.</summary>
    [HttpPost("reports/{reportId:guid}/resolve")]
    public async Task<IActionResult> ResolveReport(Guid reportId, [FromBody] ResolveReportRequest request)
    {
        var adminId = GetUserId();
        if (adminId is null)
            return Unauthorized();

        try
        {
            await _mediator.Send(new ResolveReportCommand(adminId.Value, reportId, request.Resolution, request.Note));
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Ban a user with mandatory reason, immediate session+SignalR revocation.</summary>
    [HttpPost("users/{userId:guid}/ban")]
    public async Task<IActionResult> BanUser(Guid userId, [FromBody] BanUserRequest request)
    {
        var adminId = GetUserId();
        if (adminId is null)
            return Unauthorized();

        try
        {
            await _mediator.Send(new BanUserCommand(adminId.Value, userId, request.Reason));
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Unban a user with justification.</summary>
    [HttpPost("users/{userId:guid}/unban")]
    public async Task<IActionResult> UnbanUser(Guid userId, [FromBody] UnbanUserRequest request)
    {
        var adminId = GetUserId();
        if (adminId is null)
            return Unauthorized();

        try
        {
            await _mediator.Send(new UnbanUserCommand(adminId.Value, userId, request.Reason));
            return NoContent();
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

public sealed record ResolveReportRequest(string Resolution, string Note);
public sealed record BanUserRequest(string Reason);
public sealed record UnbanUserRequest(string Reason);
