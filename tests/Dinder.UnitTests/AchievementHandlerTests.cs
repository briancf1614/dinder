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

public class AchievementHandlerTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IDiscoveryRepository> _discoveryRepoMock;
    private readonly Mock<IChatRepository> _chatRepoMock;
    private readonly Mock<IAchievementRegistry> _achievementRegistryMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<AchievementHandler>> _loggerMock;

    public AchievementHandlerTests()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _discoveryRepoMock = new Mock<IDiscoveryRepository>();
        _chatRepoMock = new Mock<IChatRepository>();
        _achievementRegistryMock = new Mock<IAchievementRegistry>();
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<AchievementHandler>>();
    }

    [Fact]
    public async Task FirstMatch_UnlocksAchievement()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUser(userId);
        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = CreateHandler();
        var @event = new MatchCreatedEvent(Guid.NewGuid(), userId, Guid.NewGuid());

        // Act
        await ((INotificationHandler<MatchCreatedEvent>)handler).Handle(@event, CancellationToken.None);

        // Assert — AchievementUnlockedEvent published
        _mediatorMock.Verify(m => m.Publish(
            It.Is<AchievementUnlockedEvent>(e => e.UserId == userId && e.Type == AchievementType.FirstMatch),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task FirstMatch_AlreadyUnlocked_IsIdempotent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUser(userId, [AchievementType.FirstMatch]);
        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = CreateHandler();
        var @event = new MatchCreatedEvent(Guid.NewGuid(), userId, Guid.NewGuid());

        // Act
        await ((INotificationHandler<MatchCreatedEvent>)handler).Handle(@event, CancellationToken.None);

        // Assert — no duplicate event
        _mediatorMock.Verify(m => m.Publish(
            It.IsAny<AchievementUnlockedEvent>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CenturySwiper_100Swipes_UnlocksAchievement()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUser(userId);
        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _discoveryRepoMock.Setup(r => r.GetLifetimeSwipeCountAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(100);

        var handler = CreateHandler();
        var @event = new SwipeRecordedEvent(Guid.NewGuid(), userId, Guid.NewGuid(), "Right");

        // Act
        await ((INotificationHandler<SwipeRecordedEvent>)handler).Handle(@event, CancellationToken.None);

        // Assert
        _mediatorMock.Verify(m => m.Publish(
            It.Is<AchievementUnlockedEvent>(e => e.UserId == userId && e.Type == AchievementType.CenturySwiper),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CenturySwiper_Under100_DoesNotUnlock()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUser(userId);
        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _discoveryRepoMock.Setup(r => r.GetLifetimeSwipeCountAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(99);

        var handler = CreateHandler();
        var @event = new SwipeRecordedEvent(Guid.NewGuid(), userId, Guid.NewGuid(), "Right");

        // Act
        await ((INotificationHandler<SwipeRecordedEvent>)handler).Handle(@event, CancellationToken.None);

        // Assert — no achievement
        _mediatorMock.Verify(m => m.Publish(
            It.IsAny<AchievementUnlockedEvent>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AchievementUnlockedEvent_PersistsToUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUser(userId);
        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = CreateHandler();
        var @event = new AchievementUnlockedEvent(userId, AchievementType.FirstMatch);

        // Act
        await ((INotificationHandler<AchievementUnlockedEvent>)handler).Handle(@event, CancellationToken.None);

        // Assert — user updated and saved
        _userRepoMock.Verify(r => r.Update(user), Times.Once);
        _userRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Contains("FirstMatch", user.Achievements);
    }

    [Fact]
    public async Task AchievementUnlockedEvent_AlreadyPersisted_IsIdempotent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUser(userId, [AchievementType.FirstMatch]);
        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = CreateHandler();

        // Act
        await ((INotificationHandler<AchievementUnlockedEvent>)handler).Handle(
            new AchievementUnlockedEvent(userId, AchievementType.FirstMatch),
            CancellationToken.None);

        // Assert — no duplicate save
        _userRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static User CreateUser(Guid userId, AchievementType[]? existingAchievements = null)
    {
        var user = new User(
            new Dinder.Domain.ValueObjects.Email("test@test.com"),
            "hash");

        // Bypass entity construction: set Id via reflection
        typeof(User).GetProperty("Id")?.SetValue(user, userId);

        if (existingAchievements is { Length: > 0 })
        {
            var json = System.Text.Json.JsonSerializer.Serialize(
                existingAchievements.Select(a => a.ToString()).ToList());
            user.SetAchievements(json);
        }

        return user;
    }

    private AchievementHandler CreateHandler()
    {
        return new AchievementHandler(
            _userRepoMock.Object,
            _discoveryRepoMock.Object,
            _chatRepoMock.Object,
            _achievementRegistryMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);
    }
}
