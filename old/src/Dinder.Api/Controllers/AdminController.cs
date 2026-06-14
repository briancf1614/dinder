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

    /// <summary>Get reports queue, optionally filtered by status and sub-category.</summary>
    [HttpGet("reports")]
    public async Task<IActionResult> GetReports(
        [FromQuery] string? status = null,
        [FromQuery] string? subCategory = null)
    {
        Domain.Enums.ReportStatus? reportStatus = null;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<Domain.Enums.ReportStatus>(status, true, out var parsed))
            reportStatus = parsed;

        var result = await _mediator.Send(new GetReportsQuery(reportStatus, subCategory));
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

    // ── AI Moderation Override ────────────────────────────────────────────

    /// <summary>Override an AI moderation decision (approve flagged or reject auto-approved photo).</summary>
    [HttpPost("photos/{mediaFileId:guid}/override")]
    public async Task<IActionResult> OverridePhotoDecision(Guid mediaFileId, [FromBody] OverridePhotoRequest request)
    {
        var adminId = GetUserId();
        if (adminId is null)
            return Unauthorized();

        try
        {
            var result = await _mediator.Send(new OverridePhotoDecisionCommand(
                adminId.Value, mediaFileId, request.Decision, request.Reason));
            return Ok(new { mediaFileId = result.MediaFileId, status = result.Status });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ── Admin Analytics ──────────────────────────────────────────────────

    /// <summary>Get daily active users (DAU) over a time range. Query: ?days=7|30|90 (default 30).</summary>
    [HttpGet("analytics/dau")]
    public async Task<IActionResult> GetDAU([FromQuery] int days = 30)
    {
        days = Math.Clamp(days, 1, 90);
        var result = await _mediator.Send(new GetAnalyticsQuery("dau", days));
        return Ok(result);
    }

    /// <summary>Get subscription conversion rate over a time range. Query: ?days=7|30|90 (default 30).</summary>
    [HttpGet("analytics/conversion")]
    public async Task<IActionResult> GetConversion([FromQuery] int days = 30)
    {
        days = Math.Clamp(days, 1, 90);
        var result = await _mediator.Send(new GetAnalyticsQuery("conversion", days));
        return Ok(result);
    }

    /// <summary>Get match rate / swipe-to-match ratio over a time range. Query: ?days=7|30|90 (default 30).</summary>
    [HttpGet("analytics/matches")]
    public async Task<IActionResult> GetMatches([FromQuery] int days = 30)
    {
        days = Math.Clamp(days, 1, 90);
        var result = await _mediator.Send(new GetAnalyticsQuery("matches", days));
        return Ok(result);
    }

    // ── Prompt Catalog ──────────────────────────────────────────────────

    /// <summary>Create a new prompt in the catalog.</summary>
    [HttpPost("prompts")]
    public async Task<IActionResult> CreatePrompt([FromBody] CreatePromptRequest request)
    {
        try
        {
            var id = await _mediator.Send(new CreatePromptCommand(request.Text, request.Category));
            return CreatedAtAction(nameof(CreatePrompt), new { id }, new { id });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Update an existing prompt (text, category, enabled/disabled).</summary>
    [HttpPut("prompts/{id:guid}")]
    public async Task<IActionResult> UpdatePrompt(Guid id, [FromBody] UpdatePromptRequest request)
    {
        try
        {
            await _mediator.Send(new UpdatePromptCommand(id, request.Text, request.Category, request.IsEnabled));
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ── Icebreaker Library ──────────────────────────────────────────────

    /// <summary>Create a new icebreaker question in the library.</summary>
    [HttpPost("icebreakers")]
    public async Task<IActionResult> CreateIcebreaker([FromBody] CreateIcebreakerRequest request)
    {
        try
        {
            var id = await _mediator.Send(new CreateIcebreakerCommand(request.Text, request.Category));
            return CreatedAtAction(nameof(CreateIcebreaker), new { id }, new { id });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Update an existing icebreaker question (text, category, enabled/disabled).</summary>
    [HttpPut("icebreakers/{id:guid}")]
    public async Task<IActionResult> UpdateIcebreaker(Guid id, [FromBody] UpdateIcebreakerRequest request)
    {
        try
        {
            await _mediator.Send(new UpdateIcebreakerCommand(id, request.Text, request.Category, request.IsEnabled));
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
public sealed record OverridePhotoRequest(Application.Admin.Commands.OverrideDecision Decision, string? Reason);
public sealed record CreatePromptRequest(string Text, Domain.Enums.PromptCategory Category);
public sealed record UpdatePromptRequest(string Text, Domain.Enums.PromptCategory Category, bool IsEnabled);
public sealed record CreateIcebreakerRequest(string Text, Domain.Enums.IcebreakerCategory Category);
public sealed record UpdateIcebreakerRequest(string Text, Domain.Enums.IcebreakerCategory Category, bool IsEnabled);
