using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Domain.Events;
using Dinder.Domain.Interfaces;
using MediatR;

namespace Dinder.Application.Discovery.Commands;

public sealed record SwipeCommand(Guid SwiperId, Guid SwipedId, SwipeDirection Direction) : IRequest<SwipeResult>;

public sealed record SwipeResult(bool IsMatch, Guid? MatchId);

public sealed class SwipeCommandHandler : IRequestHandler<SwipeCommand, SwipeResult>
{
    private readonly IDiscoveryRepository _discoveryRepository;
    private readonly IMediator _mediator;

    public SwipeCommandHandler(IDiscoveryRepository discoveryRepository, IMediator mediator)
    {
        _discoveryRepository = discoveryRepository;
        _mediator = mediator;
    }

    public async Task<SwipeResult> Handle(SwipeCommand request, CancellationToken cancellationToken)
    {
        // Check daily swipe limit
        var dailyCount = await _discoveryRepository.GetDailySwipeCountAsync(request.SwiperId, cancellationToken);
        if (dailyCount >= 50)
        {
            var resetTime = DateTime.UtcNow.Date.AddDays(1);
            throw new InvalidOperationException($"SWIPE_LIMIT_REACHED:{resetTime:O}");
        }

        // Check for existing swipe (idempotent upsert)
        var existingSwipe = await _discoveryRepository.GetSwipeAsync(request.SwiperId, request.SwipedId, cancellationToken);

        if (existingSwipe is not null)
        {
            // Update direction if different
            existingSwipe.UpdateDirection(request.Direction);
        }
        else
        {
            var swipe = new Swipe(request.SwiperId, request.SwipedId, request.Direction);
            _discoveryRepository.AddSwipe(swipe);
        }

        // Check for mutual match if swiping right
        Match? match = null;
        if (request.Direction == SwipeDirection.Right)
        {
            var reverseSwipe = await _discoveryRepository.GetSwipeAsync(request.SwipedId, request.SwiperId, cancellationToken);
            if (reverseSwipe?.Direction == SwipeDirection.Right)
            {
                // Mutual match detected — create Match + Conversation atomically
                match = new Match(request.SwiperId, request.SwipedId);
                _discoveryRepository.AddMatch(match);

                var conversation = new Conversation(match.Id);
                _discoveryRepository.AddConversation(conversation);
            }
        }

        await _discoveryRepository.SaveChangesAsync(cancellationToken);

        // Publish domain event if match was created
        if (match is not null)
        {
            await _mediator.Publish(new MatchCreatedEvent(match.Id, match.UserId1, match.UserId2), cancellationToken);
        }

        return new SwipeResult(match is not null, match?.Id);
    }
}
