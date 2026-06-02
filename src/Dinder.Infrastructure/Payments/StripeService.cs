using Dinder.Application.Common.Interfaces;
using Dinder.Application.Common.Models;
using Dinder.Domain.Enums;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace Dinder.Infrastructure.Payments;

public sealed class StripeService : IStripeService
{
    private readonly StripeConfiguration _config;

    public StripeService(IOptions<StripeConfiguration> config)
    {
        _config = config.Value;
        Stripe.StripeConfiguration.ApiKey = _config.SecretKey;
    }

    public async Task<string> CreateCheckoutSessionAsync(
        Guid userId,
        string email,
        string priceId,
        SubscriptionTier tier,
        string successUrl,
        string cancelUrl,
        CancellationToken cancellationToken = default)
    {
        var options = new SessionCreateOptions
        {
            Mode = "subscription",
            CustomerEmail = email,
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Price = priceId,
                    Quantity = 1,
                },
            ],
            Metadata = new Dictionary<string, string>
            {
                { "user_id", userId.ToString() },
                { "tier", tier.ToString() },
            },
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options, cancellationToken: cancellationToken);
        return session.Url;
    }

    public async Task<string> CreatePortalSessionAsync(
        string customerId,
        string returnUrl,
        CancellationToken cancellationToken = default)
    {
        var options = new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = customerId,
            ReturnUrl = returnUrl,
        };

        var service = new Stripe.BillingPortal.SessionService();
        var session = await service.CreateAsync(options, cancellationToken: cancellationToken);
        return session.Url;
    }

    public StripeWebhookEvent ConstructWebhookEvent(string json, string stripeSignatureHeader)
    {
        var stripeEvent = EventUtility.ConstructEvent(
            json,
            stripeSignatureHeader,
            _config.WebhookSecret);

        var webhookEvent = new StripeWebhookEvent
        {
            Id = stripeEvent.Id,
            Type = stripeEvent.Type,
            Created = stripeEvent.Created,
        };

        switch (stripeEvent.Data.Object)
        {
            case Stripe.Checkout.Session session:
                webhookEvent = webhookEvent with
                {
                    SubscriptionId = session.SubscriptionId,
                    CustomerId = session.CustomerId,
                    UserId = TryParseUserId(session.Metadata),
                    Tier = TryParseTier(session.Metadata),
                };
                break;

            case Stripe.Subscription sub:
                webhookEvent = webhookEvent with
                {
                    SubscriptionId = sub.Id,
                    CustomerId = sub.CustomerId,
                    CurrentPeriodEnd = sub.CurrentPeriodEnd,
                    Tier = ResolveTierFromSubscription(sub),
                };
                break;
        }

        return webhookEvent;
    }

    private static Guid? TryParseUserId(Dictionary<string, string> metadata)
    {
        if (metadata.TryGetValue("user_id", out var userIdStr)
            && Guid.TryParse(userIdStr, out var userId))
        {
            return userId;
        }
        return null;
    }

    private static SubscriptionTier? TryParseTier(Dictionary<string, string> metadata)
    {
        if (metadata.TryGetValue("tier", out var tierStr)
            && Enum.TryParse<SubscriptionTier>(tierStr, out var tier))
        {
            return tier;
        }
        return null;
    }

    private SubscriptionTier? ResolveTierFromSubscription(Stripe.Subscription sub)
    {
        foreach (var item in sub.Items.Data)
        {
            if (item.Price.Id == _config.Prices.Plus)
                return SubscriptionTier.Plus;
            if (item.Price.Id == _config.Prices.Premium)
                return SubscriptionTier.Premium;
        }
        return null;
    }
}
