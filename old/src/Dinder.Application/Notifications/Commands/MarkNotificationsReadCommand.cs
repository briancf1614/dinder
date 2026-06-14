using Dinder.Domain.Interfaces;
using MediatR;

namespace Dinder.Application.Notifications.Commands;

public sealed record MarkNotificationsReadCommand(Guid UserId, List<Guid>? NotificationIds = null) : IRequest<int>;

public sealed class MarkNotificationsReadCommandHandler : IRequestHandler<MarkNotificationsReadCommand, int>
{
    private readonly INotificationRepository _notificationRepository;

    public MarkNotificationsReadCommandHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<int> Handle(MarkNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        int markedCount = 0;

        if (request.NotificationIds is { Count: > 0 })
        {
            // Mark specific notifications as read
            foreach (var notificationId in request.NotificationIds)
            {
                var notification = await _notificationRepository.GetNotificationAsync(notificationId, cancellationToken);
                if (notification is not null && notification.UserId == request.UserId && !notification.IsRead)
                {
                    notification.MarkRead();
                    markedCount++;
                }
            }
        }
        else
        {
            // Mark ALL notifications as read (bulk)
            var unreadNotifications = await _notificationRepository.GetNotificationsAsync(request.UserId, null, int.MaxValue, cancellationToken);
            foreach (var notification in unreadNotifications.Where(n => !n.IsRead))
            {
                notification.MarkRead();
                markedCount++;
            }
        }

        if (markedCount > 0)
            await _notificationRepository.SaveChangesAsync(cancellationToken);

        return markedCount;
    }
}
