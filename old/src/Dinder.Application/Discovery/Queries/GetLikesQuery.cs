using Dinder.Application.Common.Attributes;
using Dinder.Domain.Enums;
using Dinder.Domain.Interfaces;
using MediatR;

namespace Dinder.Application.Discovery.Queries;

[RequiresTier(SubscriptionTier.Plus)]
public sealed record GetLikesQuery(Guid UserId) : IRequest<List<LikeDto>>;

public sealed record LikeDto(
    Guid UserId,
    DateTime LikedAt);

public sealed class GetLikesQueryHandler : IRequestHandler<GetLikesQuery, List<LikeDto>>
{
    private readonly IDiscoveryRepository _discoveryRepository;

    public GetLikesQueryHandler(IDiscoveryRepository discoveryRepository)
    {
        _discoveryRepository = discoveryRepository;
    }

    public async Task<List<LikeDto>> Handle(GetLikesQuery request, CancellationToken cancellationToken)
    {
        var likes = await _discoveryRepository.GetLikesForUserAsync(request.UserId, cancellationToken);

        return likes.Select(s => new LikeDto(s.SwiperId, s.CreatedAt)).ToList();
    }
}
