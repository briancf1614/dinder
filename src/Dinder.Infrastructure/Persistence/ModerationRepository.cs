using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Dinder.Infrastructure.Persistence;

public sealed class ModerationRepository : IModerationRepository
{
    private readonly ModerationDbContext _context;

    public ModerationRepository(ModerationDbContext context)
    {
        _context = context;
    }

    // ── Reports ─────────────────────────────────────────────────────────

    public async Task<Report?> GetReportAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Reports
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<bool> HasReportedAsync(Guid reporterId, Guid reportedUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Reports
            .AnyAsync(r => r.ReporterId == reporterId && r.ReportedUserId == reportedUserId, cancellationToken);
    }

    public async Task<List<Report>> GetReportsAsync(ReportStatus? status, string? subCategory = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Reports.AsQueryable();
        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(subCategory))
            query = query.Where(r => r.SubCategory == subCategory);

        return await query
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public void AddReport(Report report) => _context.Reports.Add(report);

    // ── Blocks ──────────────────────────────────────────────────────────

    public async Task<Block?> GetBlockAsync(Guid blockerId, Guid blockedId, CancellationToken cancellationToken = default)
    {
        return await _context.Blocks
            .FirstOrDefaultAsync(b => b.BlockerId == blockerId && b.BlockedId == blockedId, cancellationToken);
    }

    public async Task<bool> IsBlockedAsync(Guid blockerId, Guid blockedId, CancellationToken cancellationToken = default)
    {
        return await _context.Blocks
            .AnyAsync(b => b.BlockerId == blockerId && b.BlockedId == blockedId, cancellationToken);
    }

    public async Task<bool> HasBlockAsync(Guid userId1, Guid userId2, CancellationToken cancellationToken = default)
    {
        // Either direction
        return await _context.Blocks
            .AnyAsync(b =>
                (b.BlockerId == userId1 && b.BlockedId == userId2) ||
                (b.BlockerId == userId2 && b.BlockedId == userId1),
                cancellationToken);
    }

    public void AddBlock(Block block) => _context.Blocks.Add(block);

    public void RemoveBlock(Block block) => _context.Blocks.Remove(block);

    // ── Photo Reviews ───────────────────────────────────────────────────

    public async Task<PhotoReview?> GetPhotoReviewAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.PhotoReviews
            .FirstOrDefaultAsync(pr => pr.Id == id, cancellationToken);
    }

    public async Task<PhotoReview?> GetPhotoReviewByMediaFileAsync(Guid mediaFileId, CancellationToken cancellationToken = default)
    {
        return await _context.PhotoReviews
            .FirstOrDefaultAsync(pr => pr.MediaFileId == mediaFileId, cancellationToken);
    }

    public async Task<List<PhotoReview>> GetPendingPhotoReviewsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PhotoReviews
            .Where(pr => pr.Status == PhotoReviewStatus.PendingReview)
            .OrderBy(pr => pr.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public void AddPhotoReview(PhotoReview review) => _context.PhotoReviews.Add(review);

    public void UpdatePhotoReview(PhotoReview review) => _context.PhotoReviews.Update(review);

    // ── Save ────────────────────────────────────────────────────────────

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
