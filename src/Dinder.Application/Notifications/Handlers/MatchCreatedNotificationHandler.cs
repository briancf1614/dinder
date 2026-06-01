using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Domain.Events;
using Dinder.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Dinder.Application.Notifications.Handlers;

public sealed class MatchCreatedNotificationHandler : INotificationHandler<MatchCreatedEvent>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<MatchCreatedNotificationHandler> _logger;

    public MatchCreatedNotificationHandler(
        INotificationRepository notificationRepository,
        ILogger<MatchCreatedNotificationHandler> logger)
    {
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    public async Task Handle(MatchCreatedEvent notification, CancellationToken cancellationToken)
    {
        // Create in-app notifications for both users
        var notification1 = new Notification(
            notification.UserId1,
            NotificationType.Match,
            "New Match!",
            "You have a new match! Start a conversation.",
            $"dinder://chat/{notification.MatchId}");

        var notification2 = new Notification(
            notification.UserId2,
            NotificationType.Match,
            "New Match!",
            "You have a new match! Start a conversation.",
            $"dinder://chat/{notification.MatchId}");

        _notificationRepository.AddNotification(notification1);
        _notificationRepository.AddNotification(notification2);

        await _notificationRepository.SaveChangesAsync(cancellationToken);

        // Dispatch push notifications (fire-and-forget style, non-blocking)
        await DispatchPushIfNotOptedOut(notification.UserId1, notification1, cancellationToken);
        await DispatchPushIfNotOptedOut(notification.UserId2, notification2, cancellationToken);
    }

    private async Task DispatchPushIfNotOptedOut(Guid userId, Notification notification, CancellationToken cancellationToken)
    {
        try
        {
            var isOptedOut = await _notificationRepository.IsOptedOutAsync(userId, NotificationType.Match.ToString(), cancellationToken);
            if (isOptedOut)
            {
                _logger.LogDebug("User {UserId} opted out of Match push; skipping dispatch", userId);
                return;
            }

            var tokens = await _notificationRepository.GetActiveTokensForUserAsync(userId, cancellationToken);
            foreach (var deviceToken in tokens)
            {
                // Dispatch via FCM/APNs — log for MVP (actual SDK integration future work)
                _logger.LogInformation(
                    "Push dispatched: User={UserId}, Token={Token}, Platform={Platform}, Title={Title}",
                    userId, deviceToken.Token[..Math.Min(8, deviceToken.Token.Length)] + "...",
                    deviceToken.Platform, notification.Title);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch push notification for user {UserId}", userId);
        }
    }
}
