using Dinder.Domain.Enums;

namespace Dinder.Domain.Entities;

public sealed class Report
{
    public Guid Id { get; private set; }
    public Guid ReporterId { get; private set; }
    public Guid ReportedUserId { get; private set; }
    public ReportReason Reason { get; private set; }
    public string? Description { get; private set; }
    public ReportStatus Status { get; private set; }
    public string? ResolutionNote { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }

#pragma warning disable CS8618
    private Report() { } // EF Core
#pragma warning restore CS8618

    public Report(Guid reporterId, Guid reportedUserId, ReportReason reason, string? description)
    {
        Id = Guid.NewGuid();
        ReporterId = reporterId;
        ReportedUserId = reportedUserId;
        Reason = reason;
        Description = description;
        Status = ReportStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void Resolve(string note)
    {
        Status = ReportStatus.Resolved;
        ResolutionNote = note;
        ResolvedAt = DateTime.UtcNow;
    }

    public void Dismiss(string note)
    {
        Status = ReportStatus.Dismissed;
        ResolutionNote = note;
        ResolvedAt = DateTime.UtcNow;
    }
}
