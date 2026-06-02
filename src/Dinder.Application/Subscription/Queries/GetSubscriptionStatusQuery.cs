using Dinder.Domain.Enums;
using Dinder.Domain.Interfaces;
using MediatR;

namespace Dinder.Application.Subscription.Queries;

public sealed record GetSubscriptionStatusQuery(Guid UserId) : IRequest<SubscriptionStatusResult?>;

public sealed record SubscriptionStatusResult(
    SubscriptionTier Tier,
    SubscriptionStatus? Status,
    DateTime? CurrentPeriodEnd);

public sealed class GetSubscriptionStatusQueryHandler
    : IRequestHandler<GetSubscriptionStatusQuery, SubscriptionStatusResult?>
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IUserRepository _userRepository;

    public GetSubscriptionStatusQueryHandler(
        ISubscriptionRepository subscriptionRepository,
        IUserRepository userRepository)
    {
        _subscriptionRepository = subscriptionRepository;
        _userRepository = userRepository;
    }

    public async Task<SubscriptionStatusResult?> Handle(
        GetSubscriptionStatusQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return null;

        var subscription = await _subscriptionRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        return new SubscriptionStatusResult(
            user.Tier,
            subscription?.Status,
            subscription?.CurrentPeriodEnd);
    }
}
