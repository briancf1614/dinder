using Dinder.Domain.Enums;

namespace Dinder.Domain.Entities;

public sealed class Notification
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Title { get; private set; }
    public string? Body { get; private set; }
    public string? DeepLinkPayload { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime CreatedAt { get; private set; }

#pragma warning disable CS8618
    private Notification() { } // EF Core
#pragma warning restore CS8618

    public Notification(Guid userId, NotificationType type, string title, string? body = null, string? deepLinkPayload = null)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Type = type;
        Title = title;
        Body = body;
        DeepLinkPayload = deepLinkPayload;
        IsRead = false;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkRead()
    {
        IsRead = true;
    }
}
