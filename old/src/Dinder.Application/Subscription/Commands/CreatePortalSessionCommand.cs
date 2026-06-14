using Dinder.Application.Common.Interfaces;
using Dinder.Domain.Interfaces;
using MediatR;

namespace Dinder.Application.Subscription.Commands;

public sealed record CreatePortalSessionCommand(Guid UserId) : IRequest<CreatePortalSessionResult>;

public sealed record CreatePortalSessionResult(string PortalUrl);

public sealed class CreatePortalSessionCommandHandler
    : IRequestHandler<CreatePortalSessionCommand, CreatePortalSessionResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IStripeService _stripeService;

    public CreatePortalSessionCommandHandler(
        IUserRepository userRepository,
        IStripeService stripeService)
    {
        _userRepository = userRepository;
        _stripeService = stripeService;
    }

    public async Task<CreatePortalSessionResult> Handle(
        CreatePortalSessionCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            throw new InvalidOperationException("User not found.");

        if (string.IsNullOrWhiteSpace(user.StripeCustomerId))
            throw new InvalidOperationException("No Stripe customer associated with this account.");

        var returnUrl = "https://localhost:4200/subscription";

        var portalUrl = await _stripeService.CreatePortalSessionAsync(
            user.StripeCustomerId,
            returnUrl,
            cancellationToken);

        return new CreatePortalSessionResult(portalUrl);
    }
}
