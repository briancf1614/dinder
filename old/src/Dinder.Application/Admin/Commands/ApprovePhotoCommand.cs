using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Dinder.Application.Admin.Commands;

public sealed record ApprovePhotoCommand(Guid AdminId, Guid MediaFileId) : IRequest<ApprovePhotoResult>;

public sealed record ApprovePhotoResult(Guid MediaFileId, string Status, string CdnUrl);

public sealed class ApprovePhotoCommandHandler : IRequestHandler<ApprovePhotoCommand, ApprovePhotoResult>
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IModerationRepository _moderationRepository;
    private readonly IAdminRepository _adminRepository;
    private readonly IBlobStorageService _blobStorage;
    private readonly ILogger<ApprovePhotoCommandHandler> _logger;

    public ApprovePhotoCommandHandler(
        IMediaRepository mediaRepository,
        IModerationRepository moderationRepository,
        IAdminRepository adminRepository,
        IBlobStorageService blobStorage,
        ILogger<ApprovePhotoCommandHandler> logger)
    {
        _mediaRepository = mediaRepository;
        _moderationRepository = moderationRepository;
        _adminRepository = adminRepository;
        _blobStorage = blobStorage;
        _logger = logger;
    }

    public async Task<ApprovePhotoResult> Handle(ApprovePhotoCommand request, CancellationToken cancellationToken)
    {
        var mediaFile = await _mediaRepository.GetByIdAsync(request.MediaFileId, cancellationToken);
        if (mediaFile is null)
            throw new InvalidOperationException("Media file not found.");

        if (mediaFile.Status != MediaStatus.PendingReview)
            throw new InvalidOperationException("Media file is not in pending review.");

        // Approve the media file
        mediaFile.Approve(request.AdminId);

        // Update the corresponding PhotoReview
        var photoReviews = await _moderationRepository.GetPendingPhotoReviewsAsync(cancellationToken);
        var matchingReview = photoReviews.FirstOrDefault(pr => pr.MediaFileId == request.MediaFileId);
        if (matchingReview is not null)
        {
            matchingReview.Approve(request.AdminId);
            _moderationRepository.UpdatePhotoReview(matchingReview);
            await _moderationRepository.SaveChangesAsync(cancellationToken);
        }

        await _mediaRepository.SaveChangesAsync(cancellationToken);

        // Audit log
        var auditEntry = new AdminAuditLog(request.AdminId, AdminActionType.ApprovePhoto, mediaFile.OwnerId, $"Photo approved: {mediaFile.BlobKey}");
        _adminRepository.AddAuditLog(auditEntry);
        await _adminRepository.SaveChangesAsync(cancellationToken);

        var cdnUrl = _blobStorage.GetCdnUrl(mediaFile.BlobKey);

        _logger.LogInformation("Admin {AdminId} approved photo {MediaFileId} for user {OwnerId}",
            request.AdminId, request.MediaFileId, mediaFile.OwnerId);

        return new ApprovePhotoResult(mediaFile.Id, mediaFile.Status.ToString(), cdnUrl);
    }
}
