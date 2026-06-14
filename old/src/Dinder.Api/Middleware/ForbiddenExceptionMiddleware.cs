using System.Text.Json;
using Dinder.Application.Common.Exceptions;

namespace Dinder.Api.Middleware;

/// <summary>
/// Catches <see cref="ForbiddenException"/> thrown by MediatR pipeline behaviors
/// and returns HTTP 403 Forbidden with a JSON problem details body.
/// 401 = not authenticated (handled by ASP.NET auth middleware)
/// 403 = authenticated but not entitled (handled here)
/// </summary>
public sealed class ForbiddenExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public ForbiddenExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ForbiddenException ex)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";

            var response = new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                title = "Forbidden",
                status = 403,
                detail = ex.Message,
                requiredTier = ex.RequiredTier.ToString(),
                currentTier = ex.CurrentTier?.ToString()
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
