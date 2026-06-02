using System.Security.Claims;
using Dinder.Application.Common.Attributes;
using Dinder.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Dinder.Application.Common.Behaviors;

public sealed class EntitlementBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public EntitlementBehavior(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var attribute = Attribute.GetCustomAttribute(
            typeof(TRequest), typeof(RequiresTierAttribute)) as RequiresTierAttribute;

        if (attribute is null)
            return await next();

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
            return await next(); // No HTTP context (e.g., background job) — allow

        var user = httpContext.User;
        if (user.Identity?.IsAuthenticated != true)
            throw new UnauthorizedAccessException("Authentication required.");

        var tierClaim = user.FindFirstValue("tier");
        if (string.IsNullOrWhiteSpace(tierClaim)
            || !Enum.TryParse<SubscriptionTier>(tierClaim, out var userTier))
        {
            throw new UnauthorizedAccessException("Invalid or missing tier claim.");
        }

        if (userTier < attribute.MinimumTier)
        {
            throw new UnauthorizedAccessException(
                $"This feature requires at least the {attribute.MinimumTier} tier. " +
                $"Your current tier is {userTier}.");
        }

        return await next();
    }
}
