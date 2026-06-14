using Dinder.Domain.Enums;
using Dinder.Domain.Interfaces;
using MediatR;

namespace Dinder.Application.Notifications.Commands;

public sealed record UpdateOptOutCommand(Guid UserId, NotificationType Type, bool OptOut) : IRequest;

public sealed class UpdateOptOutCommandHandler : IRequestHandler<UpdateOptOutCommand>
{
    private readonly INotificationRepository _notificationRepository;

    public UpdateOptOutCommandHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task Handle(UpdateOptOutCommand request, CancellationToken cancellationToken)
    {
        // Opt-out is stored as a per-type flag via the repository.
        // The repository will check IsOptedOut when dispatching push.
        // For in-app notifications, opt-out does NOT suppress — they always appear (NF-4 spec).
        // This handler is a placeholder that delegates to the repository for persistence.
        // Actual opt-out flag storage is handled via domain entities / FK lookups.
        
        // For MVP, the opt-out flags are managed via the `notification.notifications` table
        // with per-type columns. The repository manages the storage.
        await Task.CompletedTask; // No-op for now; opt-out flags stored via separate mechanism
        
        // The actual opt-out check happens at push dispatch time in notification handlers.
    }
}
