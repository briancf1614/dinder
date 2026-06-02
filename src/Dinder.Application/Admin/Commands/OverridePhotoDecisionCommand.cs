using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Dinder.Application.Admin.Commands;

/// <summary>
/// Admin overrides an AI moderation decision (approve a flagged photo or reject an auto-approved one).
/// Records an audit log entry for traceability.
/// </summary>
public sealed record OverridePhotoDecisionCommand(
    Guid AdminId,
    Guid MediaFileId,
    OverrideDecision Decision,
    string? Reason) : IRequest<OverridePhotoDecisionResult>;

public enum OverrideDecision
{
    Approve = 1,
    Reject = 2
}

public sealed record OverridePhotoDecisionResult(Guid MediaFileId, string Status);

public sealed class OverridePhotoDecisionCommandHandler : IRequestHandler<OverridePhotoDecisionCommand, OverridePhotoDecisionResult>
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IModerationRepository _moderationRepository;
    private readonly IAdminRepository _adminRepository;
    private readonly ILogger<OverridePhotoDecisionCommandHandler> _logger;

    public OverridePhotoDecisionCommandHandler(
        IMediaRepository mediaRepository,
        IModerationRepository moderationRepository,
        IAdminRepository adminRepository,
        ILogger<OverridePhotoDecisionCommandHandler> logger)
    {
        _mediaRepository = mediaRepository;
        _moderationRepository = moderationRepository;
        _adminRepository = adminRepository;
        _logger = logger;
    }

    public async Task<OverridePhotoDecisionResult> Handle(OverridePhotoDecisionCommand request, CancellationToken cancellationToken)
    {
        var mediaFile = await _mediaRepository.GetByIdAsync(request.MediaFileId, cancellationToken);
        if (mediaFile is null)
            throw new InvalidOperationException("Media file not found.");

        switch (request.Decision)
        {
            case OverrideDecision.Approve:
                mediaFile.Approve(request.AdminId);
                break;

            case OverrideDecision.Reject:
                mediaFile.Reject();
                break;

            default:
                throw new InvalidOperationException($"Unknown override decision: {request.Decision}");
        }

        // Update the corresponding PhotoReview
        var photoReview = await _moderationRepository.GetPhotoReviewByMediaFileAsync(
            request.MediaFileId, cancellationToken);

        if (photoReview is not null)
        {
            if (request.Decision == OverrideDecision.Approve)
            {
                photoReview.Approve(request.AdminId);
            }
            else
            {
                photoReview.Reject(request.AdminId, request.Reason ?? "Overridden by admin");
            }

            _moderationRepository.UpdatePhotoReview(photoReview);
        }

        // Audit log
        var action = request.Decision == OverrideDecision.Approve
            ? AdminActionType.ApprovePhoto
            : AdminActionType.RejectPhoto;

        var auditNote = request.Decision == OverrideDecision.Approve
            ? $"AI decision overridden: Photo approved. Reason: {request.Reason ?? "N/A"}"
            : $"AI decision overridden: Photo rejected. Reason: {request.Reason ?? "N/A"}";

        var auditEntry = new AdminAuditLog(request.AdminId, action, mediaFile.OwnerId, auditNote);
        _adminRepository.AddAuditLog(auditEntry);

        await _mediaRepository.SaveChangesAsync(cancellationToken);
        await _moderationRepository.SaveChangesAsync(cancellationToken);
        await _adminRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Admin {AdminId} overrode AI decision for photo {MediaFileId}: {Decision}",
            request.AdminId, request.MediaFileId, request.Decision);

        return new OverridePhotoDecisionResult(mediaFile.Id, mediaFile.Status.ToString());
    }
}
