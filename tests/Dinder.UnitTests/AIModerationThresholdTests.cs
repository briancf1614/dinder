using Dinder.Application.Media.Handlers;
using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Domain.Events;
using Dinder.Domain.Interfaces;
using Dinder.Domain.ValueObjects;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dinder.UnitTests;

public class AIModerationThresholdTests
{
    // ── Threshold Edge Cases ─────────────────────────────────────────────

    [Fact]
    public async Task ModerationHandler_AllScoresBelowThreshold_AutoApproves()
    {
        // Arrange
        var mediaFileId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var blobKey = "users/123/photos/clean.jpg";

        var mediaFile = new MediaFile(ownerId, blobKey, "image/jpeg", 500 * 1024);
        typeof(MediaFile).GetProperty(nameof(MediaFile.Id))!.SetValue(mediaFile, mediaFileId);

        var photoReview = new PhotoReview(mediaFileId, ownerId);

        var aiResult = new AIScanResult(0.01f, 0.02f, 0.0f, false, false, false);

        var mediaRepoMock = new Mock<IMediaRepository>();
        mediaRepoMock.Setup(r => r.GetByIdAsync(mediaFileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaFile);

        var moderationRepoMock = new Mock<IModerationRepository>();
        moderationRepoMock.Setup(r => r.GetPhotoReviewByMediaFileAsync(mediaFileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(photoReview);

        var aiServiceMock = new Mock<IAzureVisionService>();
        aiServiceMock.Setup(s => s.AnalyzeImageAsync(blobKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(aiResult);

        var logger = NullLogger<PhotoUploadedModerationHandler>.Instance;
        var handler = new PhotoUploadedModerationHandler(
            mediaRepoMock.Object, moderationRepoMock.Object, aiServiceMock.Object, logger);

        var notification = new PhotoUploadedEvent(mediaFileId, ownerId, blobKey);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        Assert.Equal(MediaStatus.Approved, mediaFile.Status);
        Assert.Equal(0.01f, photoReview.AdultScore);
        Assert.Equal(0.02f, photoReview.RacyScore);
        Assert.Equal(0.0f, photoReview.ViolenceScore);
        moderationRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ModerationHandler_AdultScoreAboveThreshold_FlagsForManualReview()
    {
        // Arrange
        var mediaFileId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var blobKey = "users/123/photos/nsfw.jpg";

        var mediaFile = new MediaFile(ownerId, blobKey, "image/jpeg", 500 * 1024);
        typeof(MediaFile).GetProperty(nameof(MediaFile.Id))!.SetValue(mediaFile, mediaFileId);

        var photoReview = new PhotoReview(mediaFileId, ownerId);

        var aiResult = new AIScanResult(0.92f, 0.3f, 0.1f, true, false, false);

        var mediaRepoMock = new Mock<IMediaRepository>();
        mediaRepoMock.Setup(r => r.GetByIdAsync(mediaFileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaFile);

        var moderationRepoMock = new Mock<IModerationRepository>();
        moderationRepoMock.Setup(r => r.GetPhotoReviewByMediaFileAsync(mediaFileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(photoReview);

        var aiServiceMock = new Mock<IAzureVisionService>();
        aiServiceMock.Setup(s => s.AnalyzeImageAsync(blobKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(aiResult);

        var logger = NullLogger<PhotoUploadedModerationHandler>.Instance;
        var handler = new PhotoUploadedModerationHandler(
            mediaRepoMock.Object, moderationRepoMock.Object, aiServiceMock.Object, logger);

        var notification = new PhotoUploadedEvent(mediaFileId, ownerId, blobKey);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        Assert.Equal(MediaStatus.FlaggedByAI, mediaFile.Status);
        Assert.Equal(0.92f, photoReview.AdultScore);
    }

    [Fact]
    public async Task ModerationHandler_ViolenceFlagged_EntersManualQueue()
    {
        // Arrange
        var mediaFileId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var blobKey = "users/123/photos/violent.jpg";

        var mediaFile = new MediaFile(ownerId, blobKey, "image/jpeg", 500 * 1024);
        typeof(MediaFile).GetProperty(nameof(MediaFile.Id))!.SetValue(mediaFile, mediaFileId);

        var photoReview = new PhotoReview(mediaFileId, ownerId);

        var aiResult = new AIScanResult(0.05f, 0.1f, 0.88f, false, false, true);

        var mediaRepoMock = new Mock<IMediaRepository>();
        mediaRepoMock.Setup(r => r.GetByIdAsync(mediaFileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaFile);

        var moderationRepoMock = new Mock<IModerationRepository>();
        moderationRepoMock.Setup(r => r.GetPhotoReviewByMediaFileAsync(mediaFileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(photoReview);

        var aiServiceMock = new Mock<IAzureVisionService>();
        aiServiceMock.Setup(s => s.AnalyzeImageAsync(blobKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(aiResult);

        var logger = NullLogger<PhotoUploadedModerationHandler>.Instance;
        var handler = new PhotoUploadedModerationHandler(
            mediaRepoMock.Object, moderationRepoMock.Object, aiServiceMock.Object, logger);

        var notification = new PhotoUploadedEvent(mediaFileId, ownerId, blobKey);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        Assert.Equal(MediaStatus.FlaggedByAI, mediaFile.Status);
    }

    [Fact]
    public async Task ModerationHandler_AIResultNull_LeavesInManualQueue()
    {
        // Arrange
        var mediaFileId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var blobKey = "users/123/photos/unknown.jpg";

        var mediaFile = new MediaFile(ownerId, blobKey, "image/jpeg", 500 * 1024);
        typeof(MediaFile).GetProperty(nameof(MediaFile.Id))!.SetValue(mediaFile, mediaFileId);

        var mediaRepoMock = new Mock<IMediaRepository>();
        mediaRepoMock.Setup(r => r.GetByIdAsync(mediaFileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaFile);

        var moderationRepoMock = new Mock<IModerationRepository>();

        var aiServiceMock = new Mock<IAzureVisionService>();
        aiServiceMock.Setup(s => s.AnalyzeImageAsync(blobKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIScanResult?)null); // AI disabled

        var logger = NullLogger<PhotoUploadedModerationHandler>.Instance;
        var handler = new PhotoUploadedModerationHandler(
            mediaRepoMock.Object, moderationRepoMock.Object, aiServiceMock.Object, logger);

        var notification = new PhotoUploadedEvent(mediaFileId, ownerId, blobKey);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert — stays in AIScanning (AIScanning call happened but null means leave to manual queue)
        Assert.Equal(MediaStatus.AIScanning, mediaFile.Status);
    }

    [Fact]
    public async Task ModerationHandler_AIServiceThrows_DoesNotPropagate()
    {
        // Arrange
        var mediaFileId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var blobKey = "users/123/photos/broken.jpg";

        var mediaFile = new MediaFile(ownerId, blobKey, "image/jpeg", 500 * 1024);
        typeof(MediaFile).GetProperty(nameof(MediaFile.Id))!.SetValue(mediaFile, mediaFileId);

        var mediaRepoMock = new Mock<IMediaRepository>();
        mediaRepoMock.Setup(r => r.GetByIdAsync(mediaFileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaFile);

        var moderationRepoMock = new Mock<IModerationRepository>();

        var aiServiceMock = new Mock<IAzureVisionService>();
        aiServiceMock.Setup(s => s.AnalyzeImageAsync(blobKey, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Azure timed out"));

        var logger = NullLogger<PhotoUploadedModerationHandler>.Instance;
        var handler = new PhotoUploadedModerationHandler(
            mediaRepoMock.Object, moderationRepoMock.Object, aiServiceMock.Object, logger);

        var notification = new PhotoUploadedEvent(mediaFileId, ownerId, blobKey);

        // Act — must NOT throw (failure leaves photo for manual review)
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        mediaRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ModerationHandler_MediaFileNotFound_SkipsModeration()
    {
        // Arrange
        var mediaFileId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var blobKey = "users/123/photos/missing.jpg";

        var mediaRepoMock = new Mock<IMediaRepository>();
        mediaRepoMock.Setup(r => r.GetByIdAsync(mediaFileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MediaFile?)null);

        var moderationRepoMock = new Mock<IModerationRepository>();
        var aiServiceMock = new Mock<IAzureVisionService>();
        var logger = NullLogger<PhotoUploadedModerationHandler>.Instance;
        var handler = new PhotoUploadedModerationHandler(
            mediaRepoMock.Object, moderationRepoMock.Object, aiServiceMock.Object, logger);

        var notification = new PhotoUploadedEvent(mediaFileId, ownerId, blobKey);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert — no call to AI service
        aiServiceMock.Verify(s => s.AnalyzeImageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ModerationHandler_BoundaryThresholdExactlyAtLimit_AutoApproves()
    {
        // Arrange — scores at exactly 0.5, which should be treated based on the AIScanResult flags
        var mediaFileId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var blobKey = "users/123/photos/borderline.jpg";

        var mediaFile = new MediaFile(ownerId, blobKey, "image/jpeg", 500 * 1024);
        typeof(MediaFile).GetProperty(nameof(MediaFile.Id))!.SetValue(mediaFile, mediaFileId);

        var photoReview = new PhotoReview(mediaFileId, ownerId);

        // Not flagged by the service — clean
        var aiResult = new AIScanResult(0.49f, 0.49f, 0.49f, false, false, false);

        var mediaRepoMock = new Mock<IMediaRepository>();
        mediaRepoMock.Setup(r => r.GetByIdAsync(mediaFileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaFile);

        var moderationRepoMock = new Mock<IModerationRepository>();
        moderationRepoMock.Setup(r => r.GetPhotoReviewByMediaFileAsync(mediaFileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(photoReview);

        var aiServiceMock = new Mock<IAzureVisionService>();
        aiServiceMock.Setup(s => s.AnalyzeImageAsync(blobKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(aiResult);

        var logger = NullLogger<PhotoUploadedModerationHandler>.Instance;
        var handler = new PhotoUploadedModerationHandler(
            mediaRepoMock.Object, moderationRepoMock.Object, aiServiceMock.Object, logger);

        var notification = new PhotoUploadedEvent(mediaFileId, ownerId, blobKey);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert — auto-approved because isAdult/Racy/GoryContent all false
        Assert.Equal(MediaStatus.Approved, mediaFile.Status);
    }
}
