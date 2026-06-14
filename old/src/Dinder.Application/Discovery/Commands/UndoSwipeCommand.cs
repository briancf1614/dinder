using Dinder.Application.Common.Attributes;
using Dinder.Domain.Enums;
using Dinder.Domain.Interfaces;
using MediatR;

namespace Dinder.Application.Discovery.Commands;

[RequiresTier(SubscriptionTier.Plus)]
public sealed record UndoSwipeCommand(Guid UserId) : IRequest<UndoSwipeResult>;

public sealed record UndoSwipeResult(bool Success, string? Message);

public sealed class UndoSwipeCommandHandler : IRequestHandler<UndoSwipeCommand, UndoSwipeResult>
{
    private readonly IDiscoveryRepository _discoveryRepository;

    public UndoSwipeCommandHandler(IDiscoveryRepository discoveryRepository)
    {
        _discoveryRepository = discoveryRepository;
    }

    public async Task<UndoSwipeResult> Handle(UndoSwipeCommand request, CancellationToken cancellationToken)
    {
        var lastSwipe = await _discoveryRepository.GetLastSwipeAsync(request.UserId, cancellationToken);

        if (lastSwipe is null)
            return new UndoSwipeResult(false, "No swipes to undo.");

        _discoveryRepository.RemoveSwipe(lastSwipe);
        await _discoveryRepository.SaveChangesAsync(cancellationToken);

        return new UndoSwipeResult(true, $"Undid swipe on {lastSwipe.SwipedId}.");
    }
}
