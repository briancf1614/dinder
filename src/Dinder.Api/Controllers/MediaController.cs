using System.Security.Claims;
using Dinder.Application.Media.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dinder.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public sealed class MediaController : ControllerBase
{
    private readonly IMediator _mediator;

    public MediaController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Generate a pre-signed Azure Blob SAS PUT URL for direct client upload.</summary>
    [HttpPost("upload-url")]
    public async Task<IActionResult> GenerateUploadUrl([FromBody] GenerateUploadUrlRequest request)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        try
        {
            var result = await _mediator.Send(new GenerateUploadUrlCommand(
                userId.Value,
                request.ContentType,
                request.Extension));

            return Ok(new
            {
                uploadUrl = result.UploadUrl,
                blobKey = result.BlobKey,
                expiresAt = result.ExpiresAt
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Confirm a completed upload, triggers moderation queue.</summary>
    [HttpPost("confirm")]
    public async Task<IActionResult> ConfirmUpload([FromBody] ConfirmUploadRequest request)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        try
        {
            var result = await _mediator.Send(new ConfirmUploadCommand(
                userId.Value,
                request.BlobKey,
                request.ContentType,
                request.FileSizeBytes));

            return Ok(new
            {
                mediaFileId = result.MediaFileId,
                status = result.Status
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Appeal a rejected or AI-flagged photo decision. Re-enters the manual moderation queue.</summary>
    [HttpPost("photos/{mediaFileId:guid}/appeal")]
    public async Task<IActionResult> AppealPhoto(Guid mediaFileId, [FromBody] AppealPhotoRequest request)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized();

        try
        {
            var result = await _mediator.Send(new AppealPhotoCommand(
                userId.Value, mediaFileId, request.Reason));
            return Ok(new { mediaFileId = result.MediaFileId, status = result.Status });
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

public sealed record GenerateUploadUrlRequest(string ContentType, string Extension);
public sealed record ConfirmUploadRequest(string BlobKey, string ContentType, long FileSizeBytes);
public sealed record AppealPhotoRequest(string Reason);
