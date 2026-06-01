using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Xunit;

namespace Dinder.UnitTests;

public class AdminEntityTests
{
    [Fact]
    public void AdminAuditLog_Constructor_CreatesImmutableEntry()
    {
        var adminId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        var entry = new AdminAuditLog(adminId, AdminActionType.BanUser, targetUserId, "Repeated harassment.");

        Assert.NotEqual(Guid.Empty, entry.Id);
        Assert.Equal(adminId, entry.AdminId);
        Assert.Equal(AdminActionType.BanUser, entry.Action);
        Assert.Equal(targetUserId, entry.TargetUserId);
        Assert.Equal("Repeated harassment.", entry.Reason);
        Assert.True(entry.Timestamp <= DateTime.UtcNow);
    }

    [Fact]
    public void AdminAuditLog_UnbanAction_PersistsCorrectly()
    {
        var entry = new AdminAuditLog(Guid.NewGuid(), AdminActionType.UnbanUser, Guid.NewGuid(), "Appeal accepted.");

        Assert.Equal(AdminActionType.UnbanUser, entry.Action);
        Assert.Equal("Appeal accepted.", entry.Reason);
    }

    [Fact]
    public void AdminAuditLog_ResolveReportAction()
    {
        var entry = new AdminAuditLog(Guid.NewGuid(), AdminActionType.ResolveReport, Guid.NewGuid(), "Report confirmed.");

        Assert.Equal(AdminActionType.ResolveReport, entry.Action);
    }

    [Fact]
    public void AdminAuditLog_ApprovePhotoAction()
    {
        var entry = new AdminAuditLog(Guid.NewGuid(), AdminActionType.ApprovePhoto, Guid.NewGuid(), "Photo looks fine.");

        Assert.Equal(AdminActionType.ApprovePhoto, entry.Action);
    }

    [Fact]
    public void AdminAuditLog_WithNullTarget_Succeeds()
    {
        var entry = new AdminAuditLog(Guid.NewGuid(), AdminActionType.DismissReport, null, "System cleanup.");

        Assert.Null(entry.TargetUserId);
    }
}
