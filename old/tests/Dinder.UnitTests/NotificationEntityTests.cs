using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Xunit;

namespace Dinder.UnitTests;

public class NotificationEntityTests
{
    [Fact]
    public void Notification_Constructor_CreatesUnread()
    {
        var userId = Guid.NewGuid();
        var notification = new Notification(userId, NotificationType.Match, "New Match!", "You matched with someone!");

        Assert.NotEqual(Guid.Empty, notification.Id);
        Assert.Equal(userId, notification.UserId);
        Assert.Equal(NotificationType.Match, notification.Type);
        Assert.Equal("New Match!", notification.Title);
        Assert.Equal("You matched with someone!", notification.Body);
        Assert.False(notification.IsRead);
        Assert.True(notification.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Notification_MarkRead_SetsIsReadToTrue()
    {
        var notification = new Notification(Guid.NewGuid(), NotificationType.Message, "New Message", "You have a new message.");

        Assert.False(notification.IsRead);
        notification.MarkRead();
        Assert.True(notification.IsRead);
    }

    [Fact]
    public void DeviceToken_Constructor_CreatesActiveToken()
    {
        var userId = Guid.NewGuid();
        var token = "fcm-token-abc123";
        var platform = DevicePlatform.Fcm;

        var deviceToken = new DeviceToken(userId, token, platform);

        Assert.NotEqual(Guid.Empty, deviceToken.Id);
        Assert.Equal(userId, deviceToken.UserId);
        Assert.Equal(token, deviceToken.Token);
        Assert.Equal(platform, deviceToken.Platform);
        Assert.False(deviceToken.IsExpired);
        Assert.True(deviceToken.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void DeviceToken_MarkExpired_SetsExpired()
    {
        var deviceToken = new DeviceToken(Guid.NewGuid(), "token", DevicePlatform.Apns);

        Assert.False(deviceToken.IsExpired);
        deviceToken.MarkExpired();
        Assert.True(deviceToken.IsExpired);
    }

    [Fact]
    public void DeviceToken_ReassignUser_UpdatesUserIdAndResetsExpired()
    {
        var originalUserId = Guid.NewGuid();
        var newUserId = Guid.NewGuid();
        var deviceToken = new DeviceToken(originalUserId, "token", DevicePlatform.Fcm);
        deviceToken.MarkExpired();

        deviceToken.ReassignUser(newUserId);

        Assert.Equal(newUserId, deviceToken.UserId);
        Assert.False(deviceToken.IsExpired);
        Assert.True(deviceToken.UpdatedAt > deviceToken.CreatedAt);
    }
}
