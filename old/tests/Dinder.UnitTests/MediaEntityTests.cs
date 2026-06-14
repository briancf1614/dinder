using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Xunit;

namespace Dinder.UnitTests;

public class MediaEntityTests
{
    [Fact]
    public void MediaFile_Constructor_CreatesPendingReview()
    {
        var ownerId = Guid.NewGuid();
        var blobKey = "users/123/photos/abc.jpg";
        var contentType = "image/jpeg";
        long fileSize = 1024 * 1024;

        var mediaFile = new MediaFile(ownerId, blobKey, contentType, fileSize);

        Assert.NotEqual(Guid.Empty, mediaFile.Id);
        Assert.Equal(ownerId, mediaFile.OwnerId);
        Assert.Equal(blobKey, mediaFile.BlobKey);
        Assert.Equal(contentType, mediaFile.ContentType);
        Assert.Equal(fileSize, mediaFile.FileSizeBytes);
        Assert.Equal(MediaStatus.PendingReview, mediaFile.Status);
        Assert.True(mediaFile.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void MediaFile_Approve_ChangesStatusAndRecordsAdmin()
    {
        var mediaFile = new MediaFile(Guid.NewGuid(), "users/123/photos/photo.png", "image/png", 512000);
        var adminId = Guid.NewGuid();

        mediaFile.Approve(adminId);

        Assert.Equal(MediaStatus.Approved, mediaFile.Status);
        Assert.Equal(adminId, mediaFile.ApprovedByAdminId);
        Assert.NotNull(mediaFile.ApprovedAt);
    }

    [Fact]
    public void MediaFile_Reject_ChangesStatusToRejected()
    {
        var mediaFile = new MediaFile(Guid.NewGuid(), "users/456/photos/bad.webp", "image/webp", 2048);

        mediaFile.Reject();

        Assert.Equal(MediaStatus.Rejected, mediaFile.Status);
    }
}
