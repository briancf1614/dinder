using System.Security.Claims;
using Dinder.Application.Profile.Commands;
using Dinder.Application.Profile.Queries;
using Dinder.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dinder.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public sealed class ProfileController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProfileController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Get the current user's profile. Creates on first read.</summary>
    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        try
        {
            var result = await _mediator.Send(new GetProfileQuery(userId.Value));
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>Create or update the current user's profile.</summary>
    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        try
        {
            var result = await _mediator.Send(new CreateOrUpdateProfileCommand(
                userId.Value,
                request.DisplayName,
                request.Gender,
                request.Bio,
                request.Prompts?.Select(p => new ProfilePromptDto(p.PromptId, p.Answer)).ToList()));
            return Ok(result);
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

    /// <summary>Update profile geolocation.</summary>
    [HttpPut("location")]
    public async Task<IActionResult> UpdateLocation([FromBody] UpdateLocationRequest request)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        try
        {
            await _mediator.Send(new UpdateProfileLocationCommand(userId.Value, request.Latitude, request.Longitude));
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Replace all profile prompts (max 3, each ≤150 chars).</summary>
    [HttpPut("prompts")]
    public async Task<IActionResult> UpdatePrompts([FromBody] UpdatePromptsRequest request)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        try
        {
            await _mediator.Send(new UpdateProfilePromptsCommand(
                userId.Value,
                request.Prompts.Select(p =>
                    new PromptItem(p.PromptId, p.Answer)).ToList()));
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message switch
            {
                string m when m.StartsWith("PROMPT_LIMIT_EXCEEDED") =>
                    StatusCode(422, new { error = ex.Message }),
                string m when m.StartsWith("PROMPT_ANSWER") =>
                    BadRequest(new { error = ex.Message }),
                _ => BadRequest(new { error = ex.Message })
            };
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
    }

    /// <summary>Get the enabled prompt catalog for the current user.</summary>
    [HttpGet("prompts/catalog")]
    public async Task<IActionResult> GetPromptCatalog()
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        var result = await _mediator.Send(new GetPromptCatalogQuery());
        return Ok(result);
    }

    /// <summary>Get discovery preferences.</summary>
    [HttpGet("preferences")]
    public async Task<IActionResult> GetPreferences()
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        var result = await _mediator.Send(new GetPreferencesQuery(userId.Value));
        if (result is null)
            return NotFound(new { error = "Preferences not set." });

        return Ok(result);
    }

    /// <summary>Update discovery preferences.</summary>
    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdatePreferencesRequest request)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        try
        {
            var result = await _mediator.Send(new UpdatePreferencesCommand(
                userId.Value,
                request.InterestedInGenders,
                request.MinAge,
                request.MaxAge,
                request.MaxDistanceKm));
            return Ok(result);
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

    /// <summary>Reorder profile photos.</summary>
    [HttpPost("photos/reorder")]
    public async Task<IActionResult> ReorderPhotos([FromBody] ReorderPhotosRequest request)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        try
        {
            await _mediator.Send(new ReorderPhotosCommand(userId.Value, request.PhotoIds));
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

public sealed record UpdateProfileRequest(
    string DisplayName,
    Gender Gender,
    string? Bio,
    List<PromptRequestDto>? Prompts);

public sealed record PromptRequestDto(Guid PromptId, string Answer);

public sealed record UpdateLocationRequest(double Latitude, double Longitude);

public sealed record UpdatePreferencesRequest(
    List<Gender> InterestedInGenders,
    int MinAge,
    int MaxAge,
    int MaxDistanceKm);

public sealed record ReorderPhotosRequest(List<Guid> PhotoIds);

public sealed record UpdatePromptsRequest(List<PromptRequestDto> Prompts);
