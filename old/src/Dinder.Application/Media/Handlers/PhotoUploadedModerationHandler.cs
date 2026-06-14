using Dinder.Domain.Events;
using Dinder.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Dinder.Application.Media.Handlers;

/// <summary>
/// On photo upload confirmation, triggers AI moderation via Azure AI Vision.
/// - Transitions MediaFile to AIScanning
/// - Calls Azure AI Vision to analyze for adult/racy/violence content
/// - If clean (all scores below threshold): auto-approves, skips manual queue
/// - If flagged (any score above threshold): sets FlaggedByAI, enters manual queue
/// - If AI moderation disabled (UseAIModeration=false): leaves in PendingReview (manual queue fallback)
/// </summary>
public sealed class PhotoUploadedModerationHandler : INotificationHandler<PhotoUploadedEvent>
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IModerationRepository _moderationRepository;
    private readonly IAzureVisionService _aiVisionService;
    private readonly ILogger<PhotoUploadedModerationHandler> _logger;

    public PhotoUploadedModerationHandler(
        IMediaRepository mediaRepository,
        IModerationRepository moderationRepository,
        IAzureVisionService aiVisionService,
        ILogger<PhotoUploadedModerationHandler> logger)
    {
        _mediaRepository = mediaRepository;
        _moderationRepository = moderationRepository;
        _aiVisionService = aiVisionService;
        _logger = logger;
    }

    public async Task Handle(PhotoUploadedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Photo {MediaFileId} entered moderation pipeline (Owner: {OwnerId})",
            notification.MediaFileId, notification.OwnerId);

        var mediaFile = await _mediaRepository.GetByIdAsync(notification.MediaFileId, cancellationToken);
        if (mediaFile is null)
        {
            _logger.LogWarning("MediaFile {MediaFileId} not found — skipping moderation", notification.MediaFileId);
            return;
        }

        var photoReview = await _moderationRepository.GetPhotoReviewByMediaFileAsync(
            notification.MediaFileId, cancellationToken);

        // ── Transition to AIScanning ────────────────────────────────────
        mediaFile.SetAIScanning();

        try
        {
            // ── Call AI Vision ──────────────────────────────────────────
            var result = await _aiVisionService.AnalyzeImageAsync(notification.BlobKey, cancellationToken);

            if (result is null)
            {
                // AI moderation disabled or image unanalyzable — leave in PendingReview for manual queue
                _logger.LogInformation(
                    "AI analysis returned null for {MediaFileId} — leaving in manual queue",
                    notification.MediaFileId);
                await _mediaRepository.SaveChangesAsync(cancellationToken);
                return;
            }

            // Store AI scores on the PhotoReview
            if (photoReview is not null)
            {
                photoReview.SetAIScores(result.AdultScore, result.RacyScore, result.ViolenceScore);
                _moderationRepository.UpdatePhotoReview(photoReview);
            }

            // ── Determine if flagged ────────────────────────────────────
            var isFlagged = result.IsAdultContent || result.IsRacyContent || result.IsGoryContent;

            if (isFlagged)
            {
                // Flagged — enters manual moderation queue
                mediaFile.SetFlaggedByAI();

                _logger.LogWarning(
                    "Photo {MediaFileId} flagged by AI — Adult={Adult:F3}, Racy={Racy:F3}, Violence={Violence:F3}",
                    notification.MediaFileId, result.AdultScore, result.RacyScore, result.ViolenceScore);
            }
            else
            {
                // Clean — auto-approve
                mediaFile.AutoApprove();

                if (photoReview is not null)
                {
                    photoReview.Approve(null); // null admin = system/auto
                    _moderationRepository.UpdatePhotoReview(photoReview);
                }

                _logger.LogInformation(
                    "Photo {MediaFileId} auto-approved by AI — Adult={Adult:F3}, Racy={Racy:F3}, Violence={Violence:F3}",
                    notification.MediaFileId, result.AdultScore, result.RacyScore, result.ViolenceScore);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "AI moderation failed for {MediaFileId} — leaving in manual queue",
                notification.MediaFileId);
            // On failure, leave the photo in AIScanning/PendingReview for manual review
        }

        await _mediaRepository.SaveChangesAsync(cancellationToken);
        await _moderationRepository.SaveChangesAsync(cancellationToken);
    }
}
