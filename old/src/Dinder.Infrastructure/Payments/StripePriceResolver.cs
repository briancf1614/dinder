using Dinder.Application.Common.Interfaces;
using Dinder.Domain.Enums;
using Microsoft.Extensions.Options;

namespace Dinder.Infrastructure.Payments;

public sealed class StripePriceResolver : IStripePriceResolver
{
    private readonly StripeConfiguration _config;

    public StripePriceResolver(IOptions<StripeConfiguration> config)
    {
        _config = config.Value;
    }

    public string GetPriceId(SubscriptionTier tier) => tier switch
    {
        SubscriptionTier.Plus => _config.Prices.Plus,
        SubscriptionTier.Premium => _config.Prices.Premium,
        _ => throw new InvalidOperationException($"No price configured for tier: {tier}"),
    };
}
