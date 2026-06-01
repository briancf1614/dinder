using Dinder.Domain.Interfaces;
using MediatR;

namespace Dinder.Application.Notifications.Queries;

public sealed record GetNotificationsQuery(Guid UserId, Guid? Cursor = null, int Limit = 20) : IRequest<NotificationsResult>;

public sealed record NotificationsResult(
    List<NotificationDto> Notifications,
    Guid? NextCursor,
    int UnreadCount);

public sealed record NotificationDto(
    Guid Id,
    string Type,
    string Title,
    string? Body,
    string? DeepLinkPayload,
    bool IsRead,
    DateTime CreatedAt);

public sealed class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, NotificationsResult>
{
    private readonly INotificationRepository _notificationRepository;

    public GetNotificationsQueryHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<NotificationsResult> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var notifications = await _notificationRepository.GetNotificationsAsync(
            request.UserId,
            request.Cursor,
            request.Limit + 1,
            cancellationToken);

        var hasMore = notifications.Count > request.Limit;
        if (hasMore)
            notifications = notifications.Take(request.Limit).ToList();

        var unreadCount = await _notificationRepository.GetUnreadCountAsync(request.UserId, cancellationToken);

        var result = notifications.Select(n => new NotificationDto(
            n.Id,
            n.Type.ToString(),
            n.Title,
            n.Body,
            n.DeepLinkPayload,
            n.IsRead,
            n.CreatedAt)).ToList();

        return new NotificationsResult(result, hasMore ? notifications.Last().Id : null, unreadCount);
    }
}
