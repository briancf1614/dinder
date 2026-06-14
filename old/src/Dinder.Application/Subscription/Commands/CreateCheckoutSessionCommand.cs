using Dinder.Application.Common.Interfaces;
using Dinder.Domain.Enums;
using Dinder.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace Dinder.Application.Subscription.Commands;

public sealed record CreateCheckoutSessionCommand(
    Guid UserId,
    string Email,
    SubscriptionTier Tier) : IRequest<CreateCheckoutSessionResult>;

public sealed record CreateCheckoutSessionResult(string SessionUrl);

public sealed class CreateCheckoutSessionCommandValidator : AbstractValidator<CreateCheckoutSessionCommand>
{
    public CreateCheckoutSessionCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Tier).IsInEnum()
            .Must(t => t != SubscriptionTier.Free)
            .WithMessage("Cannot checkout for the Free tier.");
    }
}

public sealed class CreateCheckoutSessionCommandHandler
    : IRequestHandler<CreateCheckoutSessionCommand, CreateCheckoutSessionResult>
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IStripeService _stripeService;
    private readonly IStripePriceResolver _priceResolver;

    public CreateCheckoutSessionCommandHandler(
        ISubscriptionRepository subscriptionRepository,
        IStripeService stripeService,
        IStripePriceResolver priceResolver)
    {
        _subscriptionRepository = subscriptionRepository;
        _stripeService = stripeService;
        _priceResolver = priceResolver;
    }

    public async Task<CreateCheckoutSessionResult> Handle(
        CreateCheckoutSessionCommand request,
        CancellationToken cancellationToken)
    {
        // Reject if already subscribed to the same tier
        var existing = await _subscriptionRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (existing is not null && existing.Tier == request.Tier && existing.Status == SubscriptionStatus.Active)
        {
            throw new InvalidOperationException(
                $"User is already subscribed to the {request.Tier} tier.");
        }

        var priceId = _priceResolver.GetPriceId(request.Tier);

        var successUrl = "https://localhost:4200/subscription/success?session_id={CHECKOUT_SESSION_ID}";
        var cancelUrl = "https://localhost:4200/subscription/cancel";

        var sessionUrl = await _stripeService.CreateCheckoutSessionAsync(
            request.UserId,
            request.Email,
            priceId,
            request.Tier,
            successUrl,
            cancelUrl,
            cancellationToken);

        return new CreateCheckoutSessionResult(sessionUrl);
    }
}
