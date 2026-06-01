using Dinder.Domain.Entities;

namespace Dinder.Domain.Interfaces;

public interface INotificationRepository
{
    // Notifications
    void AddNotification(Notification notification);
    Task<List<Notification>> GetNotificationsAsync(Guid userId, Guid? cursor, int limit, CancellationToken cancellationToken = default);
    Task<Notification?> GetNotificationAsync(Guid notificationId, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);

    // Device tokens
    Task<DeviceToken?> GetDeviceTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<List<DeviceToken>> GetActiveTokensForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    void AddDeviceToken(DeviceToken deviceToken);
    void UpdateDeviceToken(DeviceToken deviceToken);

    // Opt-out
    Task<bool> IsOptedOutAsync(Guid userId, string notificationType, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
