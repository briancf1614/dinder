using System.Security.Claims;
using Dinder.Application.Common.Attributes;
using Dinder.Application.Common.Exceptions;
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
            throw new ForbiddenException("Authentication required.");

        var tierClaim = user.FindFirstValue("tier");
        if (string.IsNullOrWhiteSpace(tierClaim)
            || !Enum.TryParse<SubscriptionTier>(tierClaim, out var userTier))
        {
            throw new ForbiddenException("Invalid or missing tier claim.");
        }

        if (userTier < attribute.MinimumTier)
        {
            throw new ForbiddenException(attribute.MinimumTier, userTier);
        }

        return await next();
    }
}
