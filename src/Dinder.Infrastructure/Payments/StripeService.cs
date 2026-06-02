using Dinder.Application.Common.Interfaces;
using Dinder.Application.Common.Models;
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

        return new StripeWebhookEvent
        {
            Id = stripeEvent.Id,
            Type = stripeEvent.Type,
            Created = stripeEvent.Created,
            SubscriptionId = stripeEvent.Data.Object switch
            {
                Stripe.Subscription sub => sub.Id,
                Stripe.Checkout.Session session => session.SubscriptionId,
                _ => null,
            },
            CustomerId = stripeEvent.Data.Object switch
            {
                Stripe.Subscription sub => sub.CustomerId,
                Stripe.Checkout.Session session => session.CustomerId,
                _ => null,
            },
        };
    }
}
