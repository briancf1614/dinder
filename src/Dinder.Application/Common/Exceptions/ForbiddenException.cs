using Dinder.Domain.Enums;

namespace Dinder.Application.Common.Exceptions;

/// <summary>
/// Thrown when an authenticated user lacks the required subscription tier
/// for a gated feature. Mapped to HTTP 403 Forbidden by the API layer.
/// </summary>
public sealed class ForbiddenException : Exception
{
    public SubscriptionTier RequiredTier { get; }
    public SubscriptionTier? CurrentTier { get; }

    public ForbiddenException(SubscriptionTier requiredTier, SubscriptionTier? currentTier)
        : base($"This feature requires at least the {requiredTier} tier. " +
               (currentTier.HasValue
                   ? $"Your current tier is {currentTier}."
                   : "No tier was found in your token."))
    {
        RequiredTier = requiredTier;
        CurrentTier = currentTier;
    }

    public ForbiddenException(string message) : base(message)
    {
    }
}
