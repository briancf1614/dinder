using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Dinder.Infrastructure.SignalR;

[Authorize]
public sealed class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId is not null)
        {
            // Add user to their personal notification group
            await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroup(userId.Value));
        }

        _logger.LogDebug("User {UserId} connected to NotificationHub (Connection: {ConnectionId})",
            userId, Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId is not null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetUserGroup(userId.Value));
        }
        _logger.LogDebug("User {UserId} disconnected from NotificationHub", userId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Send a new notification to a specific user.</summary>
    public static async Task SendNotificationAsync(IHubContext<NotificationHub> hubContext, Guid userId, object notification, ILogger logger)
    {
        await hubContext.Clients.Group(GetUserGroup(userId))
            .SendAsync("NewNotification", notification);
        logger.LogDebug("Notification sent to user {UserId}", userId);
    }

    /// <summary>Update the badge count for a specific user.</summary>
    public static async Task SendBadgeUpdateAsync(IHubContext<NotificationHub> hubContext, Guid userId, int unreadCount, ILogger logger)
    {
        await hubContext.Clients.Group(GetUserGroup(userId))
            .SendAsync("BadgeUpdate", new { unreadCount });
        logger.LogDebug("Badge update sent to user {UserId}: {Count} unread", userId, unreadCount);
    }

    private Guid? GetUserId()
    {
        var userIdClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirstValue("sub");

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private static string GetUserGroup(Guid userId) => $"user_{userId}";
}
