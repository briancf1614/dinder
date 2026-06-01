using Dinder.Domain.Enums;

namespace Dinder.Domain.Entities;

public sealed class PhotoReview
{
    public Guid Id { get; private set; }
    public Guid MediaFileId { get; private set; }
    public Guid UserId { get; private set; }
    public PhotoReviewStatus Status { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public Guid? ReviewedByAdminId { get; private set; }

#pragma warning disable CS8618
    private PhotoReview() { } // EF Core
#pragma warning restore CS8618

    public PhotoReview(Guid mediaFileId, Guid userId)
    {
        Id = Guid.NewGuid();
        MediaFileId = mediaFileId;
        UserId = userId;
        Status = PhotoReviewStatus.PendingReview;
        CreatedAt = DateTime.UtcNow;
    }

    public void Approve(Guid adminId)
    {
        Status = PhotoReviewStatus.Approved;
        ReviewedByAdminId = adminId;
        ReviewedAt = DateTime.UtcNow;
    }

    public void Reject(Guid adminId, string reason)
    {
        Status = PhotoReviewStatus.Rejected;
        RejectionReason = reason;
        ReviewedByAdminId = adminId;
        ReviewedAt = DateTime.UtcNow;
    }
}
