using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Dinder.Application.Admin.Commands;

public sealed record RejectPhotoCommand(Guid AdminId, Guid MediaFileId, string Reason) : IRequest<RejectPhotoResult>;

public sealed record RejectPhotoResult(Guid MediaFileId, string Status);

public sealed class RejectPhotoCommandHandler : IRequestHandler<RejectPhotoCommand, RejectPhotoResult>
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IModerationRepository _moderationRepository;
    private readonly IAdminRepository _adminRepository;
    private readonly ILogger<RejectPhotoCommandHandler> _logger;

    public RejectPhotoCommandHandler(
        IMediaRepository mediaRepository,
        IModerationRepository moderationRepository,
        IAdminRepository adminRepository,
        ILogger<RejectPhotoCommandHandler> logger)
    {
        _mediaRepository = mediaRepository;
        _moderationRepository = moderationRepository;
        _adminRepository = adminRepository;
        _logger = logger;
    }

    public async Task<RejectPhotoResult> Handle(RejectPhotoCommand request, CancellationToken cancellationToken)
    {
        var mediaFile = await _mediaRepository.GetByIdAsync(request.MediaFileId, cancellationToken);
        if (mediaFile is null)
            throw new InvalidOperationException("Media file not found.");

        if (mediaFile.Status != MediaStatus.PendingReview)
            throw new InvalidOperationException("Media file is not in pending review.");

        // Reject the media file (file is NOT deleted per SM-3 spec)
        mediaFile.Reject();

        // Update the corresponding PhotoReview
        var photoReviews = await _moderationRepository.GetPendingPhotoReviewsAsync(cancellationToken);
        var matchingReview = photoReviews.FirstOrDefault(pr => pr.MediaFileId == request.MediaFileId);
        if (matchingReview is not null)
        {
            matchingReview.Reject(request.AdminId, request.Reason);
            _moderationRepository.UpdatePhotoReview(matchingReview);
            await _moderationRepository.SaveChangesAsync(cancellationToken);
        }

        await _mediaRepository.SaveChangesAsync(cancellationToken);

        // Audit log
        var auditEntry = new AdminAuditLog(request.AdminId, AdminActionType.RejectPhoto, mediaFile.OwnerId,
            $"Photo rejected: {mediaFile.BlobKey}. Reason: {request.Reason}");
        _adminRepository.AddAuditLog(auditEntry);
        await _adminRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Admin {AdminId} rejected photo {MediaFileId} for user {OwnerId}: {Reason}",
            request.AdminId, request.MediaFileId, mediaFile.OwnerId, request.Reason);

        return new RejectPhotoResult(mediaFile.Id, mediaFile.Status.ToString());
    }
}
