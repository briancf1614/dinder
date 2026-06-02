using Dinder.Domain.Interfaces;
using MediatR;

namespace Dinder.Application.Discovery.Queries;

public sealed record GetMatchesQuery(Guid UserId) : IRequest<List<MatchDto>>;

public sealed record MatchDto(
    Guid MatchId,
    Guid MatchedUserId,
    Guid? ConversationId,
    DateTime CreatedAt,
    string? IcebreakerQuestion,
    string? IcebreakerCategory);

public sealed class GetMatchesQueryHandler : IRequestHandler<GetMatchesQuery, List<MatchDto>>
{
    private readonly IDiscoveryRepository _discoveryRepository;

    public GetMatchesQueryHandler(IDiscoveryRepository discoveryRepository)
    {
        _discoveryRepository = discoveryRepository;
    }

    public async Task<List<MatchDto>> Handle(GetMatchesQuery request, CancellationToken cancellationToken)
    {
        var matches = await _discoveryRepository.GetMatchesForUserAsync(request.UserId, cancellationToken);

        return matches.Select(m => new MatchDto(
            m.Id,
            m.UserId1 == request.UserId ? m.UserId2 : m.UserId1,
            m.Conversation?.Id,
            m.CreatedAt,
            m.Conversation?.IcebreakerQuestion,
            m.Conversation?.IcebreakerCategory?.ToString())).ToList();
    }
}
