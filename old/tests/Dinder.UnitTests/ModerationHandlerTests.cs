using Dinder.Application.Moderation.Commands;
using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Domain.Interfaces;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dinder.UnitTests;

public class ModerationHandlerTests
{
    [Fact]
    public async Task ReportUserCommand_ValidInput_CreatesReport()
    {
        // Arrange
        var reporterId = Guid.NewGuid();
        var reportedUserId = Guid.NewGuid();
        var reason = ReportReason.Harassment;

        var moderationRepoMock = new Mock<IModerationRepository>();
        var userRepoMock = new Mock<IUserRepository>();
        userRepoMock.Setup(x => x.GetByIdAsync(reportedUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User(
                new Domain.ValueObjects.Email("reported@test.com"),
                "hash",
                new DateOnly(1990, 1, 1)));
        moderationRepoMock.Setup(x => x.HasReportedAsync(reporterId, reportedUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var logger = NullLogger<ReportUserCommandHandler>.Instance;
        var handler = new ReportUserCommandHandler(moderationRepoMock.Object, userRepoMock.Object, logger);

        // Act
        var result = await handler.Handle(new ReportUserCommand(reporterId, reportedUserId, reason, null, "Bad behavior"), CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result.ReportId);
        Assert.False(result.IsDuplicate);
        moderationRepoMock.Verify(x => x.AddReport(It.IsAny<Report>()), Times.Once);
        moderationRepoMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReportUserCommand_SelfReport_ThrowsInvalidOperation()
    {
        var userId = Guid.NewGuid();
        var moderationRepoMock = new Mock<IModerationRepository>();
        var userRepoMock = new Mock<IUserRepository>();
        var logger = NullLogger<ReportUserCommandHandler>.Instance;
        var handler = new ReportUserCommandHandler(moderationRepoMock.Object, userRepoMock.Object, logger);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new ReportUserCommand(userId, userId, ReportReason.Spam, null, "I'm bad"), CancellationToken.None));

        Assert.Contains("yourself", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReportUserCommand_DuplicateReport_StillAllows()
    {
        var reporterId = Guid.NewGuid();
        var reportedUserId = Guid.NewGuid();

        var moderationRepoMock = new Mock<IModerationRepository>();
        var userRepoMock = new Mock<IUserRepository>();
        userRepoMock.Setup(x => x.GetByIdAsync(reportedUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User(
                new Domain.ValueObjects.Email("reported@test.com"),
                "hash",
                new DateOnly(1990, 1, 1)));
        moderationRepoMock.Setup(x => x.HasReportedAsync(reporterId, reportedUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // Already reported

        var logger = NullLogger<ReportUserCommandHandler>.Instance;
        var handler = new ReportUserCommandHandler(moderationRepoMock.Object, userRepoMock.Object, logger);

        var result = await handler.Handle(
            new ReportUserCommand(reporterId, reportedUserId, ReportReason.Harassment, null, "Second report"),
            CancellationToken.None);

        Assert.True(result.IsDuplicate);
        moderationRepoMock.Verify(x => x.AddReport(It.IsAny<Report>()), Times.Once); // Still creates report
    }

    [Fact]
    public async Task BlockUserCommand_ValidInput_CreatesBlock()
    {
        var blockerId = Guid.NewGuid();
        var blockedId = Guid.NewGuid();

        var moderationRepoMock = new Mock<IModerationRepository>();
        var userRepoMock = new Mock<IUserRepository>();
        userRepoMock.Setup(x => x.GetByIdAsync(blockedId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User(
                new Domain.ValueObjects.Email("blocked@test.com"),
                "hash",
                new DateOnly(1995, 5, 5)));
        moderationRepoMock.Setup(x => x.GetBlockAsync(blockerId, blockedId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Block?)null);

        var logger = NullLogger<BlockUserCommandHandler>.Instance;
        var handler = new BlockUserCommandHandler(moderationRepoMock.Object, userRepoMock.Object, logger);

        var result = await handler.Handle(new BlockUserCommand(blockerId, blockedId), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.BlockId);
        Assert.False(result.AlreadyBlocked);
        moderationRepoMock.Verify(x => x.AddBlock(It.IsAny<Block>()), Times.Once);
    }

    [Fact]
    public async Task BlockUserCommand_AlreadyBlocked_ReturnsExisting()
    {
        var blockerId = Guid.NewGuid();
        var blockedId = Guid.NewGuid();
        var existingBlock = new Block(blockerId, blockedId);

        var moderationRepoMock = new Mock<IModerationRepository>();
        var userRepoMock = new Mock<IUserRepository>();
        userRepoMock.Setup(x => x.GetByIdAsync(blockedId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User(
                new Domain.ValueObjects.Email("blocked@test.com"),
                "hash",
                new DateOnly(1995, 5, 5)));
        moderationRepoMock.Setup(x => x.GetBlockAsync(blockerId, blockedId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBlock);

        var logger = NullLogger<BlockUserCommandHandler>.Instance;
        var handler = new BlockUserCommandHandler(moderationRepoMock.Object, userRepoMock.Object, logger);

        var result = await handler.Handle(new BlockUserCommand(blockerId, blockedId), CancellationToken.None);

        Assert.Equal(existingBlock.Id, result.BlockId);
        Assert.True(result.AlreadyBlocked);
        moderationRepoMock.Verify(x => x.AddBlock(It.IsAny<Block>()), Times.Never); // No new block
    }

    [Fact]
    public async Task BlockUserCommand_SelfBlock_ThrowsInvalidOperation()
    {
        var userId = Guid.NewGuid();
        var moderationRepoMock = new Mock<IModerationRepository>();
        var userRepoMock = new Mock<IUserRepository>();
        var logger = NullLogger<BlockUserCommandHandler>.Instance;
        var handler = new BlockUserCommandHandler(moderationRepoMock.Object, userRepoMock.Object, logger);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new BlockUserCommand(userId, userId), CancellationToken.None));

        Assert.Contains("yourself", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BanUserCommand_BansActiveUser()
    {
        var adminId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var user = new User(
            new Domain.ValueObjects.Email("target@test.com"),
            "hash",
            new DateOnly(1990, 1, 1));

        var userRepoMock = new Mock<IUserRepository>();
        userRepoMock.Setup(x => x.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var adminRepoMock = new Mock<IAdminRepository>();
        var mediatorMock = new Mock<MediatR.IMediator>();
        var logger = NullLogger<BanUserCommandHandler>.Instance;

        var handler = new BanUserCommandHandler(userRepoMock.Object, adminRepoMock.Object, mediatorMock.Object, logger);

        await handler.Handle(new BanUserCommand(adminId, targetUserId, "Harassment"), CancellationToken.None);

        Assert.Equal(AccountStatus.Banned, user.Status);
        Assert.Equal("Harassment", user.BanReason);
        adminRepoMock.Verify(x => x.AddAuditLog(It.IsAny<AdminAuditLog>()), Times.Once);
        mediatorMock.Verify(x => x.Publish(It.IsAny<Domain.Events.UserBannedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BanUserCommand_AlreadyBanned_Throws()
    {
        var adminId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var user = new User(
            new Domain.ValueObjects.Email("target@test.com"),
            "hash",
            new DateOnly(1990, 1, 1));
        user.Ban("Already banned");

        var userRepoMock = new Mock<IUserRepository>();
        userRepoMock.Setup(x => x.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var adminRepoMock = new Mock<IAdminRepository>();
        var mediatorMock = new Mock<MediatR.IMediator>();
        var logger = NullLogger<BanUserCommandHandler>.Instance;

        var handler = new BanUserCommandHandler(userRepoMock.Object, adminRepoMock.Object, mediatorMock.Object, logger);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new BanUserCommand(adminId, targetUserId, "Again"), CancellationToken.None));

        Assert.Contains("already banned", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnbanUserCommand_UnbansCorrectly()
    {
        var adminId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var user = new User(
            new Domain.ValueObjects.Email("target@test.com"),
            "hash",
            new DateOnly(1990, 1, 1));
        user.Ban("Test ban");

        var userRepoMock = new Mock<IUserRepository>();
        userRepoMock.Setup(x => x.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var adminRepoMock = new Mock<IAdminRepository>();
        var logger = NullLogger<UnbanUserCommandHandler>.Instance;

        var handler = new UnbanUserCommandHandler(userRepoMock.Object, adminRepoMock.Object, logger);

        await handler.Handle(new UnbanUserCommand(adminId, targetUserId, "Appeal accepted"), CancellationToken.None);

        Assert.Equal(AccountStatus.Active, user.Status);
        Assert.Null(user.BanReason);
        adminRepoMock.Verify(x => x.AddAuditLog(It.IsAny<AdminAuditLog>()), Times.Once);
    }

    [Fact]
    public async Task UnbanUserCommand_NotBanned_Throws()
    {
        var adminId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var user = new User(
            new Domain.ValueObjects.Email("target@test.com"),
            "hash",
            new DateOnly(1990, 1, 1));
        // User is Active, not Banned

        var userRepoMock = new Mock<IUserRepository>();
        userRepoMock.Setup(x => x.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var adminRepoMock = new Mock<IAdminRepository>();
        var logger = NullLogger<UnbanUserCommandHandler>.Instance;

        var handler = new UnbanUserCommandHandler(userRepoMock.Object, adminRepoMock.Object, logger);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new UnbanUserCommand(adminId, targetUserId, "Why?"), CancellationToken.None));

        Assert.Contains("not banned", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
