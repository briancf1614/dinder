using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Domain.Events;
using Dinder.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Dinder.Application.Notifications.Handlers;

public sealed class MessageSentNotificationHandler : INotificationHandler<MessageSentEvent>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<MessageSentNotificationHandler> _logger;

    public MessageSentNotificationHandler(
        INotificationRepository notificationRepository,
        ILogger<MessageSentNotificationHandler> logger)
    {
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    public async Task Handle(MessageSentEvent notification, CancellationToken cancellationToken)
    {
        // Create in-app notification for the recipient
        var appNotification = new Notification(
            notification.RecipientId,
            NotificationType.Message,
            "New Message",
            notification.ContentPreview,
            $"dinder://chat/{notification.ConversationId}");

        _notificationRepository.AddNotification(appNotification);

        await _notificationRepository.SaveChangesAsync(cancellationToken);

        // Dispatch push notification if not opted out
        await DispatchPushIfNotOptedOut(notification.RecipientId, appNotification, cancellationToken);
    }

    private async Task DispatchPushIfNotOptedOut(Guid userId, Notification notification, CancellationToken cancellationToken)
    {
        try
        {
            var isOptedOut = await _notificationRepository.IsOptedOutAsync(userId, NotificationType.Message.ToString(), cancellationToken);
            if (isOptedOut)
            {
                _logger.LogDebug("User {UserId} opted out of Message push; skipping dispatch", userId);
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
