namespace Dinder.Infrastructure.Payments;

public sealed class StripeConfiguration
{
    public const string SectionName = "Stripe";

    public string SecretKey { get; init; } = string.Empty;
    public string WebhookSecret { get; init; } = string.Empty;
    public StripePrices Prices { get; init; } = new();
}

public sealed class StripePrices
{
    public string Plus { get; init; } = string.Empty;
    public string Premium { get; init; } = string.Empty;
}
