using Dinder.Domain.Events;
using Dinder.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Dinder.Application.Media.Handlers;

public sealed class PhotoUploadedModerationHandler : INotificationHandler<PhotoUploadedEvent>
{
    private readonly IMediaRepository _mediaRepository;
    private readonly ILogger<PhotoUploadedModerationHandler> _logger;

    public PhotoUploadedModerationHandler(
        IMediaRepository mediaRepository,
        ILogger<PhotoUploadedModerationHandler> logger)
    {
        _mediaRepository = mediaRepository;
        _logger = logger;
    }

    public async Task Handle(PhotoUploadedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Photo {MediaFileId} queued for moderation (Owner: {OwnerId})",
            notification.MediaFileId, notification.OwnerId);

        // The photo is already in PendingReview status via the PhotoReview entity
        // Added by ConfirmUploadCommandHandler. This handler serves as a hook
        // for future async processing (e.g., Azure Content Moderator integration).

        // For MVP: log and complete. Admin manually reviews via the admin dashboard.
        await Task.CompletedTask;
    }
}
