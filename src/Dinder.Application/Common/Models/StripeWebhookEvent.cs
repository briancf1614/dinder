namespace Dinder.Application.Common.Models;

public sealed class StripeWebhookEvent
{
    public string Id { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string? SubscriptionId { get; init; }
    public string? CustomerId { get; init; }
    public DateTime Created { get; init; }
}
