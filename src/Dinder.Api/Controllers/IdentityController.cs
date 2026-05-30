using System.Security.Claims;
using Dinder.Application.Identity.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dinder.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class IdentityController : ControllerBase
{
    private readonly IMediator _mediator;

    public IdentityController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Register a new account with email and password.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var result = await _mediator.Send(new RegisterCommand(request.Email, request.Password));
            return Ok(new
            {
                userId = result.UserId,
                accessToken = result.AccessToken,
                refreshToken = result.RefreshToken
            });
        }
        catch (InvalidOperationException ex) when (ex.Message == "EMAIL_UNAVAILABLE")
        {
            return Conflict(new { error = "Email unavailable." });
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(e => e.ErrorMessage) });
        }
    }

    /// <summary>Login with email and password.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _mediator.Send(new LoginCommand(request.Email, request.Password));
            return Ok(new
            {
                userId = result.UserId,
                accessToken = result.AccessToken,
                refreshToken = result.RefreshToken
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    /// <summary>Login via external provider (Google, Apple).</summary>
    [HttpPost("login/external")]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalLogin([FromBody] ExternalLoginRequest request)
    {
        try
        {
            var result = await _mediator.Send(new ExternalLoginCommand(
                request.Email,
                request.Provider,
                request.ProviderUserId));
            return Ok(new
            {
                userId = result.UserId,
                accessToken = result.AccessToken,
                refreshToken = result.RefreshToken
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    /// <summary>Refresh an access token using a refresh token.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        try
        {
            var result = await _mediator.Send(new RefreshTokenCommand(request.RefreshToken));
            return Ok(new
            {
                accessToken = result.AccessToken,
                refreshToken = result.RefreshToken
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    /// <summary>Delete account (GDPR). Requires authentication.</summary>
    [HttpDelete("account")]
    [Authorize]
    public async Task<IActionResult> DeleteAccount()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        await _mediator.Send(new DeleteAccountCommand(userId));
        return NoContent();
    }
}

// Request DTOs (inline until moved to Contracts)
public sealed record RegisterRequest(string Email, string Password);
public sealed record LoginRequest(string Email, string Password);
public sealed record ExternalLoginRequest(string Email, Dinder.Domain.Enums.ExternalProvider Provider, string ProviderUserId);
public sealed record RefreshRequest(string RefreshToken);
