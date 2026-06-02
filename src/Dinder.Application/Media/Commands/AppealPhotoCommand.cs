using Dinder.Domain.Enums;
using Dinder.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Dinder.Application.Media.Commands;

/// <summary>
/// User appeals a rejected photo decision. The photo re-enters the manual moderation
/// queue for human review.
/// </summary>
public sealed record AppealPhotoCommand(Guid UserId, Guid MediaFileId, string Reason) : IRequest<AppealPhotoResult>;

public sealed record AppealPhotoResult(Guid MediaFileId, string Status);

public sealed class AppealPhotoCommandHandler : IRequestHandler<AppealPhotoCommand, AppealPhotoResult>
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IModerationRepository _moderationRepository;
    private readonly ILogger<AppealPhotoCommandHandler> _logger;

    public AppealPhotoCommandHandler(
        IMediaRepository mediaRepository,
        IModerationRepository moderationRepository,
        ILogger<AppealPhotoCommandHandler> logger)
    {
        _mediaRepository = mediaRepository;
        _moderationRepository = moderationRepository;
        _logger = logger;
    }

    public async Task<AppealPhotoResult> Handle(AppealPhotoCommand request, CancellationToken cancellationToken)
    {
        var mediaFile = await _mediaRepository.GetByIdAsync(request.MediaFileId, cancellationToken);
        if (mediaFile is null)
            throw new InvalidOperationException("Media file not found.");

        if (mediaFile.OwnerId != request.UserId)
            throw new InvalidOperationException("You can only appeal your own photos.");

        if (mediaFile.Status is not MediaStatus.Rejected and not MediaStatus.FlaggedByAI)
            throw new InvalidOperationException("Only rejected or AI-flagged photos can be appealed.");

        // Re-enter manual queue
        mediaFile.SetAIScanning();

        var photoReview = await _moderationRepository.GetPhotoReviewByMediaFileAsync(
            request.MediaFileId, cancellationToken);

        if (photoReview is not null)
        {
            // Reset review status for re-evaluation
            _moderationRepository.UpdatePhotoReview(photoReview);
        }

        await _mediaRepository.SaveChangesAsync(cancellationToken);
        await _moderationRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "User {UserId} appealed photo {MediaFileId} — reason: {Reason}",
            request.UserId, request.MediaFileId, request.Reason);

        return new AppealPhotoResult(mediaFile.Id, mediaFile.Status.ToString());
    }
}
