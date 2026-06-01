using Dinder.Domain.Entities;
using Dinder.Domain.Events;
using Dinder.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Dinder.Application.Media.Commands;

public sealed record ConfirmUploadCommand(Guid UserId, string BlobKey, string ContentType, long FileSizeBytes) : IRequest<ConfirmUploadResult>;

public sealed record ConfirmUploadResult(Guid MediaFileId, string Status);

public sealed class ConfirmUploadCommandHandler : IRequestHandler<ConfirmUploadCommand, ConfirmUploadResult>
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IModerationRepository _moderationRepository;
    private readonly IBlobStorageService _blobStorage;
    private readonly IMediator _mediator;
    private readonly ILogger<ConfirmUploadCommandHandler> _logger;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp"
    };

    public ConfirmUploadCommandHandler(
        IMediaRepository mediaRepository,
        IModerationRepository moderationRepository,
        IBlobStorageService blobStorage,
        IMediator mediator,
        ILogger<ConfirmUploadCommandHandler> logger)
    {
        _mediaRepository = mediaRepository;
        _moderationRepository = moderationRepository;
        _blobStorage = blobStorage;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<ConfirmUploadResult> Handle(ConfirmUploadCommand request, CancellationToken cancellationToken)
    {
        // Validate content type
        if (!AllowedContentTypes.Contains(request.ContentType))
            throw new InvalidOperationException($"Content type '{request.ContentType}' not allowed.");

        // Verify blob existence via SDK
        var exists = await _blobStorage.BlobExistsAsync(request.BlobKey, cancellationToken);
        if (!exists)
            throw new InvalidOperationException("Blob not found. Upload may not have completed.");

        // Check for duplicate blob key
        var existing = await _mediaRepository.GetByBlobKeyAsync(request.BlobKey, cancellationToken);
        if (existing is not null)
            throw new InvalidOperationException("This upload has already been confirmed.");

        // Validate file size ≤ 10MB
        if (request.FileSizeBytes > 10 * 1024 * 1024)
            throw new InvalidOperationException("File size exceeds 10 MB limit.");

        // Create MediaFile with PendingReview status
        var mediaFile = new MediaFile(request.UserId, request.BlobKey, request.ContentType, request.FileSizeBytes);
        _mediaRepository.Add(mediaFile);

        // Create PhotoReview for moderation queue
        var photoReview = new PhotoReview(mediaFile.Id, request.UserId);
        _moderationRepository.AddPhotoReview(photoReview);

        await _mediaRepository.SaveChangesAsync(cancellationToken);
        await _moderationRepository.SaveChangesAsync(cancellationToken);

        // Publish event to trigger moderation queue
        await _mediator.Publish(new PhotoUploadedEvent(mediaFile.Id, request.UserId, request.BlobKey), cancellationToken);

        _logger.LogInformation("Media upload confirmed: {MediaFileId}, Owner: {UserId}, Blob: {BlobKey}",
            mediaFile.Id, request.UserId, request.BlobKey);

        return new ConfirmUploadResult(mediaFile.Id, mediaFile.Status.ToString());
    }
}
