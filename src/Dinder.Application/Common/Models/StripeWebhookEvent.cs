using Dinder.Domain.Enums;

namespace Dinder.Application.Common.Models;

public sealed record StripeWebhookEvent
{
    public string Id { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string? SubscriptionId { get; init; }
    public string? CustomerId { get; init; }
    public Guid? UserId { get; init; }
    public SubscriptionTier? Tier { get; init; }
    public DateTime? CurrentPeriodEnd { get; init; }
    public DateTime Created { get; init; }
}
