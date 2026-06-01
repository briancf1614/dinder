using Dinder.Domain.Entities;
using Dinder.Domain.Enums;

namespace Dinder.Domain.Interfaces;

public interface IModerationRepository
{
    // Reports
    Task<Report?> GetReportAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasReportedAsync(Guid reporterId, Guid reportedUserId, CancellationToken cancellationToken = default);
    Task<List<Report>> GetReportsAsync(ReportStatus? status, CancellationToken cancellationToken = default);
    void AddReport(Report report);

    // Blocks
    Task<Block?> GetBlockAsync(Guid blockerId, Guid blockedId, CancellationToken cancellationToken = default);
    Task<bool> IsBlockedAsync(Guid blockerId, Guid blockedId, CancellationToken cancellationToken = default);
    Task<bool> HasBlockAsync(Guid userId1, Guid userId2, CancellationToken cancellationToken = default);
    void AddBlock(Block block);
    void RemoveBlock(Block block);

    // Photo Reviews
    Task<PhotoReview?> GetPhotoReviewAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<PhotoReview>> GetPendingPhotoReviewsAsync(CancellationToken cancellationToken = default);
    void AddPhotoReview(PhotoReview review);
    void UpdatePhotoReview(PhotoReview review);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
