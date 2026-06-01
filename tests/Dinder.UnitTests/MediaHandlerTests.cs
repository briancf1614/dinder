using Dinder.Application.Media.Commands;
using Dinder.Domain.Entities;
using Dinder.Domain.Interfaces;
using Dinder.Infrastructure.Storage;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dinder.UnitTests;

public class MediaHandlerTests
{
    [Fact]
    public async Task ConfirmUploadCommand_ValidInput_CreatesMediaFileAndPhotoReview()
    {
        var userId = Guid.NewGuid();
        var blobKey = "users/123/photos/photo.jpg";
        var contentType = "image/jpeg";
        long fileSize = 500 * 1024; // 500 KB

        var mediaRepoMock = new Mock<IMediaRepository>();
        var moderationRepoMock = new Mock<IModerationRepository>();
        var blobStorageMock = new Mock<IBlobStorageService>();
        var mediatorMock = new Mock<MediatR.IMediator>();
        var logger = NullLogger<ConfirmUploadCommandHandler>.Instance;

        blobStorageMock.Setup(x => x.BlobExistsAsync(blobKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mediaRepoMock.Setup(x => x.GetByBlobKeyAsync(blobKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MediaFile?)null);

        var handler = new ConfirmUploadCommandHandler(
            mediaRepoMock.Object,
            moderationRepoMock.Object,
            blobStorageMock.Object,
            mediatorMock.Object,
            logger);

        var result = await handler.Handle(
            new ConfirmUploadCommand(userId, blobKey, contentType, fileSize),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.MediaFileId);
        Assert.Equal("PendingReview", result.Status);
        mediaRepoMock.Verify(x => x.Add(It.IsAny<MediaFile>()), Times.Once);
        moderationRepoMock.Verify(x => x.AddPhotoReview(It.IsAny<PhotoReview>()), Times.Once);
        mediatorMock.Verify(x => x.Publish(It.IsAny<Domain.Events.PhotoUploadedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmUploadCommand_NonexistentBlob_Throws()
    {
        var blobKey = "users/123/photos/missing.jpg";

        var mediaRepoMock = new Mock<IMediaRepository>();
        var moderationRepoMock = new Mock<IModerationRepository>();
        var blobStorageMock = new Mock<IBlobStorageService>();
        var mediatorMock = new Mock<MediatR.IMediator>();
        var logger = NullLogger<ConfirmUploadCommandHandler>.Instance;

        blobStorageMock.Setup(x => x.BlobExistsAsync(blobKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new ConfirmUploadCommandHandler(
            mediaRepoMock.Object,
            moderationRepoMock.Object,
            blobStorageMock.Object,
            mediatorMock.Object,
            logger);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new ConfirmUploadCommand(Guid.NewGuid(), blobKey, "image/jpeg", 1024), CancellationToken.None));

        Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConfirmUploadCommand_UnsupportedContentType_Throws()
    {
        var blobKey = "users/123/photos/unsupported.gif";

        var mediaRepoMock = new Mock<IMediaRepository>();
        var moderationRepoMock = new Mock<IModerationRepository>();
        var blobStorageMock = new Mock<IBlobStorageService>();
        var mediatorMock = new Mock<MediatR.IMediator>();
        var logger = NullLogger<ConfirmUploadCommandHandler>.Instance;

        blobStorageMock.Setup(x => x.BlobExistsAsync(blobKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new ConfirmUploadCommandHandler(
            mediaRepoMock.Object,
            moderationRepoMock.Object,
            blobStorageMock.Object,
            mediatorMock.Object,
            logger);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new ConfirmUploadCommand(Guid.NewGuid(), blobKey, "image/gif", 1024), CancellationToken.None));

        Assert.Contains("not allowed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConfirmUploadCommand_OversizedFile_Throws()
    {
        var blobKey = "users/123/photos/too-large.jpg";
        long oversized = 11 * 1024 * 1024; // 11 MB

        var mediaRepoMock = new Mock<IMediaRepository>();
        var moderationRepoMock = new Mock<IModerationRepository>();
        var blobStorageMock = new Mock<IBlobStorageService>();
        var mediatorMock = new Mock<MediatR.IMediator>();
        var logger = NullLogger<ConfirmUploadCommandHandler>.Instance;

        blobStorageMock.Setup(x => x.BlobExistsAsync(blobKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new ConfirmUploadCommandHandler(
            mediaRepoMock.Object,
            moderationRepoMock.Object,
            blobStorageMock.Object,
            mediatorMock.Object,
            logger);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new ConfirmUploadCommand(Guid.NewGuid(), blobKey, "image/jpeg", oversized), CancellationToken.None));

        Assert.Contains("10 MB", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
