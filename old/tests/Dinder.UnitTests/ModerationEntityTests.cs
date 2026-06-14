using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Xunit;

namespace Dinder.UnitTests;

public class ModerationEntityTests
{
    [Fact]
    public void Report_Constructor_CreatesPendingReport()
    {
        var reporterId = Guid.NewGuid();
        var reportedUserId = Guid.NewGuid();

        var report = new Report(reporterId, reportedUserId, ReportReason.Harassment, null, "They were harassing me.");

        Assert.NotEqual(Guid.Empty, report.Id);
        Assert.Equal(reporterId, report.ReporterId);
        Assert.Equal(reportedUserId, report.ReportedUserId);
        Assert.Equal(ReportReason.Harassment, report.Reason);
        Assert.Equal("They were harassing me.", report.Description);
        Assert.Equal(ReportStatus.Pending, report.Status);
        Assert.True(report.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Report_Resolve_SetsStatusAndResolutionNote()
    {
        var report = new Report(Guid.NewGuid(), Guid.NewGuid(), ReportReason.Spam, null, null);

        report.Resolve("Spam confirmed, user banned.");

        Assert.Equal(ReportStatus.Resolved, report.Status);
        Assert.Equal("Spam confirmed, user banned.", report.ResolutionNote);
        Assert.NotNull(report.ResolvedAt);
    }

    [Fact]
    public void Report_Dismiss_SetsDismissedStatus()
    {
        var report = new Report(Guid.NewGuid(), Guid.NewGuid(), ReportReason.Other, null, null);

        report.Dismiss("No action needed.");

        Assert.Equal(ReportStatus.Dismissed, report.Status);
        Assert.Equal("No action needed.", report.ResolutionNote);
        Assert.NotNull(report.ResolvedAt);
    }

    [Fact]
    public void Report_FakeProfileReason_PersistsCorrectly()
    {
        var reporterId = Guid.NewGuid();
        var report = new Report(reporterId, Guid.NewGuid(), ReportReason.FakeProfile, null, "This is a catfish account.");

        Assert.Equal(ReportReason.FakeProfile, report.Reason);
        Assert.Equal("This is a catfish account.", report.Description);
    }

    [Fact]
    public void Block_Constructor_CreatesActiveBlock()
    {
        var blockerId = Guid.NewGuid();
        var blockedId = Guid.NewGuid();

        var block = new Block(blockerId, blockedId);

        Assert.NotEqual(Guid.Empty, block.Id);
        Assert.Equal(blockerId, block.BlockerId);
        Assert.Equal(blockedId, block.BlockedId);
        Assert.True(block.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void PhotoReview_Constructor_CreatesPendingReview()
    {
        var mediaFileId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var review = new PhotoReview(mediaFileId, userId);

        Assert.NotEqual(Guid.Empty, review.Id);
        Assert.Equal(mediaFileId, review.MediaFileId);
        Assert.Equal(userId, review.UserId);
        Assert.Equal(PhotoReviewStatus.PendingReview, review.Status);
    }

    [Fact]
    public void PhotoReview_Approve_SetsApprovedStatus()
    {
        var review = new PhotoReview(Guid.NewGuid(), Guid.NewGuid());
        var adminId = Guid.NewGuid();

        review.Approve(adminId);

        Assert.Equal(PhotoReviewStatus.Approved, review.Status);
        Assert.Equal(adminId, review.ReviewedByAdminId);
        Assert.NotNull(review.ReviewedAt);
    }

    [Fact]
    public void PhotoReview_Reject_SetsRejectedWithReason()
    {
        var review = new PhotoReview(Guid.NewGuid(), Guid.NewGuid());
        var adminId = Guid.NewGuid();

        review.Reject(adminId, "Nudity detected.");

        Assert.Equal(PhotoReviewStatus.Rejected, review.Status);
        Assert.Equal("Nudity detected.", review.RejectionReason);
        Assert.Equal(adminId, review.ReviewedByAdminId);
        Assert.NotNull(review.ReviewedAt);
    }
}
