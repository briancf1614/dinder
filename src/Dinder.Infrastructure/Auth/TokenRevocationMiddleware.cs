using System.Security.Claims;
using Dinder.Domain.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Dinder.Infrastructure.Auth;

public sealed class TokenRevocationMiddleware
{
    private readonly RequestDelegate _next;

    public TokenRevocationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IUserRepository userRepository)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? context.User.FindFirstValue("sub");

            if (Guid.TryParse(userIdClaim, out var userId))
            {
                var user = await userRepository.GetByIdAsync(userId, context.RequestAborted);

                if (user is null || !user.CanAuthenticate())
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"error\":\"Account is banned or deleted.\"}");
                    return;
                }
            }
        }

        await _next(context);
    }
}
