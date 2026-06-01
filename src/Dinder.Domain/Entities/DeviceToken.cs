using Dinder.Domain.Enums;

namespace Dinder.Domain.Entities;

public sealed class DeviceToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Token { get; private set; }
    public DevicePlatform Platform { get; private set; }
    public bool IsExpired { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

#pragma warning disable CS8618
    private DeviceToken() { } // EF Core
#pragma warning restore CS8618

    public DeviceToken(Guid userId, string token, DevicePlatform platform)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Token = token;
        Platform = platform;
        IsExpired = false;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkExpired()
    {
        IsExpired = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReassignUser(Guid newUserId)
    {
        UserId = newUserId;
        IsExpired = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
