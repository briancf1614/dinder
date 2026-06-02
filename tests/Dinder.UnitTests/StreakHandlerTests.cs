using Dinder.Application.Gamification.Handlers;
using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Domain.Events;
using Dinder.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Dinder.UnitTests;

public class StreakHandlerTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IAchievementRegistry> _achievementRegistryMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<StreakHandler>> _loggerMock;

    public StreakHandlerTests()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _achievementRegistryMock = new Mock<IAchievementRegistry>();
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<StreakHandler>>();
    }

    [Fact]
    public async Task ConsecutiveLogin_IncrementsStreak()
    {
        // Arrange
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var user = CreateUser(streak: 3, lastStreakDate: yesterday);
        _userRepoMock.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = CreateHandler();
        var @event = new UserLoggedInEvent(user.Id, DateTime.UtcNow);

        // Act
        await handler.Handle(@event, CancellationToken.None);

        // Assert
        _userRepoMock.Verify(r => r.Update(user), Times.Once);
        _userRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(4, user.DailyStreak);
    }

    [Fact]
    public async Task MissedDay_ResetsStreakTo1()
    {
        // Arrange
        var threeDaysAgo = DateTime.UtcNow.Date.AddDays(-3);
        var user = CreateUser(streak: 5, lastStreakDate: threeDaysAgo);
        _userRepoMock.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = CreateHandler();
        var @event = new UserLoggedInEvent(user.Id, DateTime.UtcNow);

        // Act
        await handler.Handle(@event, CancellationToken.None);

        // Assert
        Assert.Equal(1, user.DailyStreak); // Reset to 1
    }

    [Fact]
    public async Task LoginOnly_SameDay_DoesNotIncrement()
    {
        // Arrange
        var today = DateTime.UtcNow;
        var user = CreateUser(streak: 3, lastStreakDate: today);
        _userRepoMock.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = CreateHandler();
        var @event = new UserLoggedInEvent(user.Id, today);

        // Act
        await handler.Handle(@event, CancellationToken.None);

        // Assert
        Assert.Equal(3, user.DailyStreak); // No change — already processed today
    }

    [Fact]
    public async Task StreakAt30_PublishesStreakMasterAchievement()
    {
        // Arrange
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var user = CreateUser(streak: 30, lastStreakDate: yesterday);
        _userRepoMock.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = CreateHandler();
        var @event = new UserLoggedInEvent(user.Id, DateTime.UtcNow);

        // Act
        await handler.Handle(@event, CancellationToken.None);

        // Assert
        Assert.Equal(30, user.DailyStreak); // Capped
        _mediatorMock.Verify(m => m.Publish(
            It.Is<AchievementUnlockedEvent>(e => e.UserId == user.Id && e.Type == AchievementType.StreakMaster),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StreakOver30_CappedAt30()
    {
        // Arrange
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var user = CreateUser(streak: 30, lastStreakDate: yesterday); // Already at 30
        _userRepoMock.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = CreateHandler();
        var @event = new UserLoggedInEvent(user.Id, DateTime.UtcNow);

        // Act
        await handler.Handle(@event, CancellationToken.None);

        // Assert
        Assert.Equal(30, user.DailyStreak); // Still capped at 30
    }

    [Fact]
    public async Task UserNotFound_LogsWarningAndReturns()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = CreateHandler();
        var @event = new UserLoggedInEvent(userId, DateTime.UtcNow);

        // Act
        await handler.Handle(@event, CancellationToken.None);

        // Assert
        _userRepoMock.Verify(r => r.Update(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task RepositoryError_DoesNotThrow()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var handler = CreateHandler();
        var @event = new UserLoggedInEvent(Guid.NewGuid(), DateTime.UtcNow);

        // Act — should not throw (fire-and-forget)
        await handler.Handle(@event, CancellationToken.None);

        // Assert — no exception thrown
        Assert.True(true);
    }

    [Fact]
    public async Task FirstLoginEver_InitializesStreakTo1()
    {
        // Arrange
        var user = CreateUser(streak: 0, lastStreakDate: null);
        _userRepoMock.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = CreateHandler();
        var @event = new UserLoggedInEvent(user.Id, DateTime.UtcNow);

        // Act
        await handler.Handle(@event, CancellationToken.None);

        // Assert
        Assert.Equal(1, user.DailyStreak);
    }

    private static User CreateUser(int streak, DateTime? lastStreakDate)
    {
        var user = new User(
            new Dinder.Domain.ValueObjects.Email("test@test.com"),
            "hash");

        if (streak > 0 && lastStreakDate.HasValue)
        {
            // Build streak backwards: start N days before the target last streak date
            // so the final UpdateStreak call lands on lastStreakDate with the correct value.
            var startDate = lastStreakDate.Value.Date.AddDays(-(streak - 1));
            user.UpdateStreak(startDate, false); // Day 1
            for (int i = 1; i < streak; i++)
            {
                user.UpdateStreak(startDate.AddDays(i), true);
            }
        }

        return user;
    }

    private StreakHandler CreateHandler()
    {
        return new StreakHandler(
            _userRepoMock.Object,
            _achievementRegistryMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);
    }
}
