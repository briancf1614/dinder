using Dinder.Domain.Entities;
using Dinder.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Dinder.Infrastructure.Persistence;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _notificationContext;

    public NotificationRepository(NotificationDbContext notificationContext)
    {
        _notificationContext = notificationContext;
    }

    // ── Notifications ───────────────────────────────────────────────────

    public void AddNotification(Notification notification)
    {
        _notificationContext.Notifications.Add(notification);
    }

    public async Task<List<Notification>> GetNotificationsAsync(Guid userId, Guid? cursor, int limit, CancellationToken cancellationToken = default)
    {
        var query = _notificationContext.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt) // Newest first
            .ThenByDescending(n => n.Id);

        if (cursor.HasValue)
        {
            var cursorNotification = await _notificationContext.Notifications
                .FirstOrDefaultAsync(n => n.Id == cursor.Value, cancellationToken);
            if (cursorNotification is not null)
            {
                query = (IOrderedQueryable<Notification>)query.Where(n =>
                    n.CreatedAt < cursorNotification.CreatedAt ||
                    (n.CreatedAt == cursorNotification.CreatedAt && n.Id.CompareTo(cursorNotification.Id) < 0));
            }
        }

        return await query.Take(limit).ToListAsync(cancellationToken);
    }

    public async Task<Notification?> GetNotificationAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        return await _notificationContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _notificationContext.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
    }

    // ── Device Tokens ───────────────────────────────────────────────────

    public async Task<DeviceToken?> GetDeviceTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _notificationContext.DeviceTokens
            .FirstOrDefaultAsync(dt => dt.Token == token, cancellationToken);
    }

    public async Task<List<DeviceToken>> GetActiveTokensForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _notificationContext.DeviceTokens
            .Where(dt => dt.UserId == userId && !dt.IsExpired)
            .ToListAsync(cancellationToken);
    }

    public void AddDeviceToken(DeviceToken deviceToken)
    {
        _notificationContext.DeviceTokens.Add(deviceToken);
    }

    public void UpdateDeviceToken(DeviceToken deviceToken)
    {
        _notificationContext.DeviceTokens.Update(deviceToken);
    }

    // ── Opt-out ─────────────────────────────────────────────────────────

    public async Task<bool> IsOptedOutAsync(Guid userId, string notificationType, CancellationToken cancellationToken = default)
    {
        // MVP: opt-out flags are managed externally. Default is NOT opted out.
        // Future: add a NotificationPreferences table/entity for per-type opt-out.
        // For now, always return false (never opted out) until the opt-out entity is fully built.
        await Task.CompletedTask;
        return false;
    }

    // ── Save ────────────────────────────────────────────────────────────

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _notificationContext.SaveChangesAsync(cancellationToken);
    }
}
