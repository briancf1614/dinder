using Dinder.Domain.Enums;

namespace Dinder.Domain.Entities;

public sealed class AdminAuditLog
{
    public Guid Id { get; private set; }
    public Guid AdminId { get; private set; }
    public AdminActionType Action { get; private set; }
    public Guid? TargetUserId { get; private set; }
    public string Reason { get; private set; }
    public DateTime Timestamp { get; private set; }

#pragma warning disable CS8618
    private AdminAuditLog() { } // EF Core
#pragma warning restore CS8618

    public AdminAuditLog(Guid adminId, AdminActionType action, Guid? targetUserId, string reason)
    {
        Id = Guid.NewGuid();
        AdminId = adminId;
        Action = action;
        TargetUserId = targetUserId;
        Reason = reason;
        Timestamp = DateTime.UtcNow;
    }
}
