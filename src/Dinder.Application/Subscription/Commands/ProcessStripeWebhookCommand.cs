using Dinder.Application.Common.Interfaces;
using Dinder.Application.Common.Models;
using Dinder.Domain.Enums;
using Dinder.Domain.Interfaces;
using MediatR;
using SubscriptionEntity = Dinder.Domain.Entities.Subscription;

namespace Dinder.Application.Subscription.Commands;

public sealed record ProcessStripeWebhookCommand(
    string Json,
    string StripeSignatureHeader) : IRequest;

public sealed class ProcessStripeWebhookCommandHandler
    : IRequestHandler<ProcessStripeWebhookCommand>
{
    private readonly IStripeService _stripeService;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IUserRepository _userRepository;

    public ProcessStripeWebhookCommandHandler(
        IStripeService stripeService,
        ISubscriptionRepository subscriptionRepository,
        IUserRepository userRepository)
    {
        _stripeService = stripeService;
        _subscriptionRepository = subscriptionRepository;
        _userRepository = userRepository;
    }

    public async Task Handle(ProcessStripeWebhookCommand request, CancellationToken cancellationToken)
    {
        var webhookEvent = _stripeService.ConstructWebhookEvent(
            request.Json, request.StripeSignatureHeader);

        switch (webhookEvent.Type)
        {
            case "checkout.session.completed":
                await HandleCheckoutCompleted(webhookEvent, cancellationToken);
                break;

            case "customer.subscription.updated":
                await HandleSubscriptionUpdated(webhookEvent, cancellationToken);
                break;

            case "customer.subscription.deleted":
                await HandleSubscriptionDeleted(webhookEvent, cancellationToken);
                break;

            default:
                // Unhandled event type — no-op
                break;
        }
    }

    private async Task HandleCheckoutCompleted(
        StripeWebhookEvent webhookEvent,
        CancellationToken cancellationToken)
    {
        if (webhookEvent.UserId is null || webhookEvent.Tier is null
            || webhookEvent.SubscriptionId is null || webhookEvent.CustomerId is null)
        {
            return; // Incomplete event, skip
        }

        // Idempotency: check if subscription already processed
        var existing = await _subscriptionRepository.GetByStripeSubscriptionIdAsync(
            webhookEvent.SubscriptionId, cancellationToken);

        if (existing is not null)
            return; // Already processed, idempotent no-op

        var user = await _userRepository.GetByIdAsync(webhookEvent.UserId.Value, cancellationToken);
        if (user is null)
            return;

        // Create subscription record
        var subscription = new SubscriptionEntity(
            webhookEvent.UserId.Value,
            webhookEvent.SubscriptionId,
            webhookEvent.Tier.Value,
            DateTime.UtcNow.AddMonths(1));

        _subscriptionRepository.Add(subscription);

        // Update user tier and Stripe customer ID
        user.SetTier(webhookEvent.Tier.Value);
        user.SetStripeCustomerId(webhookEvent.CustomerId);
        _userRepository.Update(user);

        await _subscriptionRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task HandleSubscriptionUpdated(
        StripeWebhookEvent webhookEvent,
        CancellationToken cancellationToken)
    {
        if (webhookEvent.SubscriptionId is null)
            return;

        var subscription = await _subscriptionRepository.GetByStripeSubscriptionIdAsync(
            webhookEvent.SubscriptionId, cancellationToken);

        if (subscription is null)
            return;

        // Sync current period end if provided
        if (webhookEvent.CurrentPeriodEnd.HasValue)
        {
            subscription.UpdatePeriodEnd(webhookEvent.CurrentPeriodEnd.Value);
        }

        // Handle status progression via Stripe subscription status
        // Stripe sends subscription status changes as customer.subscription.updated
        // The tier from the Stripe event reflects the current subscription tier
        if (webhookEvent.Tier.HasValue && webhookEvent.Tier.Value != subscription.Tier)
        {
            subscription.Activate(webhookEvent.Tier.Value,
                webhookEvent.CurrentPeriodEnd ?? subscription.CurrentPeriodEnd);
        }

        // Check grace period for past_due subscriptions
        if (subscription.IsGracePeriodExceeded())
        {
            subscription.Expire();

            // Revert user to Free tier
            await RevertUserToFree(subscription.UserId, cancellationToken);
        }

        _subscriptionRepository.Update(subscription);
        await _subscriptionRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task HandleSubscriptionDeleted(
        StripeWebhookEvent webhookEvent,
        CancellationToken cancellationToken)
    {
        if (webhookEvent.SubscriptionId is null)
            return;

        var subscription = await _subscriptionRepository.GetByStripeSubscriptionIdAsync(
            webhookEvent.SubscriptionId, cancellationToken);

        if (subscription is null)
            return;

        // Idempotency: if already canceled, skip
        if (subscription.Status == SubscriptionStatus.Canceled)
            return;

        subscription.Cancel();
        _subscriptionRepository.Update(subscription);

        // Revert user to Free tier
        await RevertUserToFree(subscription.UserId, cancellationToken);

        await _subscriptionRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task RevertUserToFree(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is not null && user.Tier != SubscriptionTier.Free)
        {
            user.SetTier(SubscriptionTier.Free);
            _userRepository.Update(user);
        }
    }
}
