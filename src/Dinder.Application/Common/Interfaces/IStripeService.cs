using Dinder.Application.Common.Models;

namespace Dinder.Application.Common.Interfaces;

public interface IStripeService
{
    Task<string> CreateCheckoutSessionAsync(
        Guid userId,
        string email,
        string priceId,
        string successUrl,
        string cancelUrl,
        CancellationToken cancellationToken = default);

    Task<string> CreatePortalSessionAsync(
        string customerId,
        string returnUrl,
        CancellationToken cancellationToken = default);

    StripeWebhookEvent ConstructWebhookEvent(string json, string stripeSignatureHeader);
}
