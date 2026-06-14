using Dinder.Domain.Events;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Dinder.Infrastructure.SignalR;

/// <summary>
/// Infrastructure handler that terminates SignalR connections for banned users.
/// Lives in Infrastructure because it depends on concrete SignalR hub types.
/// </summary>
public sealed class UserBannedSignalRHandler : INotificationHandler<UserBannedEvent>
{
    private readonly IHubContext<ChatHub> _chatHubContext;
    private readonly IHubContext<NotificationHub> _notificationHubContext;
    private readonly ILogger<UserBannedSignalRHandler> _logger;

    public UserBannedSignalRHandler(
        IHubContext<ChatHub> chatHubContext,
        IHubContext<NotificationHub> notificationHubContext,
        ILogger<UserBannedSignalRHandler> logger)
    {
        _chatHubContext = chatHubContext;
        _notificationHubContext = notificationHubContext;
        _logger = logger;
    }

    public async Task Handle(UserBannedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Terminating SignalR connections for banned user {UserId}", notification.UserId);

        // Send forced disconnect message to the user's notification group
        await _notificationHubContext.Clients.Group($"user_{notification.UserId}")
            .SendAsync("ForceDisconnect", new
            {
                reason = "AccountBanned",
                message = "Your account has been banned by an administrator."
            }, cancellationToken);

        // Also send to chat user group for forceful disconnect
        await _chatHubContext.Clients.Group($"user_{notification.UserId}")
            .SendAsync("ForceDisconnect", new
            {
                reason = "AccountBanned",
                message = "Your account has been banned by an administrator."
            }, cancellationToken);

        _logger.LogWarning("Ban disconnect messages sent for user {UserId}", notification.UserId);
    }
}
