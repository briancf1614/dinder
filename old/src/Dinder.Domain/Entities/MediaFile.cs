using Dinder.Domain.Enums;

namespace Dinder.Domain.Entities;

public sealed class MediaFile
{
    public Guid Id { get; private set; }
    public Guid OwnerId { get; private set; }
    public string BlobKey { get; private set; }
    public string ContentType { get; private set; }
    public long FileSizeBytes { get; private set; }
    public MediaStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public Guid? ApprovedByAdminId { get; private set; }

#pragma warning disable CS8618
    private MediaFile() { } // EF Core
#pragma warning restore CS8618

    public MediaFile(Guid ownerId, string blobKey, string contentType, long fileSizeBytes)
    {
        Id = Guid.NewGuid();
        OwnerId = ownerId;
        BlobKey = blobKey;
        ContentType = contentType;
        FileSizeBytes = fileSizeBytes;
        Status = MediaStatus.PendingReview;
        CreatedAt = DateTime.UtcNow;
    }

    public void Approve(Guid adminId)
    {
        Status = MediaStatus.Approved;
        ApprovedByAdminId = adminId;
        ApprovedAt = DateTime.UtcNow;
    }

    public void AutoApprove()
    {
        Status = MediaStatus.Approved;
        ApprovedByAdminId = null;
        ApprovedAt = DateTime.UtcNow;
    }

    public void SetAIScanning()
    {
        Status = MediaStatus.AIScanning;
    }

    public void SetFlaggedByAI()
    {
        Status = MediaStatus.FlaggedByAI;
    }

    public void Reject()
    {
        Status = MediaStatus.Rejected;
    }
}
