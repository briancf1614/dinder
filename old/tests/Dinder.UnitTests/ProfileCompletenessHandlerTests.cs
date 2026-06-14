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

public class ProfileCompletenessHandlerTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IProfileRepository> _profileRepoMock;
    private readonly Mock<IAchievementRegistry> _achievementRegistryMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<ProfileCompletenessHandler>> _loggerMock;

    public ProfileCompletenessHandlerTests()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _profileRepoMock = new Mock<IProfileRepository>();
        _achievementRegistryMock = new Mock<IAchievementRegistry>();
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<ProfileCompletenessHandler>>();
    }

    [Fact]
    public async Task PartialProfile_UpdatesScoreTo50()
    {
        // Arrange — profile with photo and bio only (50% complete)
        var userId = Guid.NewGuid();
        var user = CreateUser(userId);
        var profile = CreateProfile(userId, hasPhoto: true, hasBio: true, hasPreferences: false, hasPrompts: false);

        _profileRepoMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = CreateHandler();
        var @event = new ProfileUpdatedEvent(userId, DateTime.UtcNow);

        // Act
        await handler.Handle(@event, CancellationToken.None);

        // Assert
        _userRepoMock.Verify(r => r.Update(user), Times.Once);
        _userRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(50, user.ProfileCompletenessScore);
        // No achievement unlocked at 50%
        _mediatorMock.Verify(m => m.Publish(
            It.IsAny<AchievementUnlockedEvent>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task FullyCompleteProfile_UpdatesScoreTo100_UnlocksAchievement()
    {
        // Arrange — all four factors present (100%)
        var userId = Guid.NewGuid();
        var user = CreateUser(userId);
        var profile = CreateProfile(userId, hasPhoto: true, hasBio: true, hasPreferences: true, hasPrompts: true);

        _profileRepoMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = CreateHandler();
        var @event = new ProfileUpdatedEvent(userId, DateTime.UtcNow);

        // Act
        await handler.Handle(@event, CancellationToken.None);

        // Assert
        Assert.Equal(100, user.ProfileCompletenessScore);
        _mediatorMock.Verify(m => m.Publish(
            It.Is<AchievementUnlockedEvent>(e => e.UserId == userId && e.Type == AchievementType.ProfileComplete),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProfileComplete_AlreadyUnlocked_DoesNotReAward()
    {
        // Arrange — user already has ProfileComplete achievement
        var userId = Guid.NewGuid();
        var user = CreateUser(userId, [AchievementType.ProfileComplete]);
        var profile = CreateProfile(userId, hasPhoto: true, hasBio: true, hasPreferences: true, hasPrompts: true);

        _profileRepoMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = CreateHandler();
        var @event = new ProfileUpdatedEvent(userId, DateTime.UtcNow);

        // Act
        await handler.Handle(@event, CancellationToken.None);

        // Assert
        Assert.Equal(100, user.ProfileCompletenessScore);
        // No duplicate achievement event
        _mediatorMock.Verify(m => m.Publish(
            It.IsAny<AchievementUnlockedEvent>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PhotoUploaded_TriggersCompletenessEvaluation()
    {
        // Arrange — PhotoUploadedEvent with OwnerId as UserId
        var userId = Guid.NewGuid();
        var user = CreateUser(userId);
        var profile = CreateProfile(userId, hasPhoto: true, hasBio: true, hasPreferences: true, hasPrompts: false);

        _profileRepoMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = CreateHandler();
        var @event = new PhotoUploadedEvent(Guid.NewGuid(), userId, "blob-key");

        // Act
        await handler.Handle(@event, CancellationToken.None);

        // Assert
        Assert.Equal(75, user.ProfileCompletenessScore);
    }

    [Fact]
    public async Task ProfileNotFound_LogsWarningAndReturns()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _profileRepoMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Profile?)null);

        var handler = CreateHandler();
        var @event = new ProfileUpdatedEvent(userId, DateTime.UtcNow);

        // Act
        await handler.Handle(@event, CancellationToken.None);

        // Assert — no user update attempted
        _userRepoMock.Verify(r => r.Update(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task UserNotFound_DoesNotThrow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = CreateProfile(userId, hasPhoto: true, hasBio: false, hasPreferences: false, hasPrompts: false);

        _profileRepoMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var handler = CreateHandler();
        var @event = new ProfileUpdatedEvent(userId, DateTime.UtcNow);

        // Act — should not throw (fire-and-forget)
        await handler.Handle(@event, CancellationToken.None);

        // Assert — no exception
        Assert.True(true);
    }

    [Fact]
    public async Task RepositoryError_DoesNotThrow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _profileRepoMock.Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB error"));

        var handler = CreateHandler();
        var @event = new ProfileUpdatedEvent(userId, DateTime.UtcNow);

        // Act — fire-and-forget, should not throw
        await handler.Handle(@event, CancellationToken.None);

        // Assert — no exception thrown
        Assert.True(true);
    }

    private static User CreateUser(Guid userId, AchievementType[]? existingAchievements = null)
    {
        var user = new User(
            new Dinder.Domain.ValueObjects.Email("test@test.com"),
            "hash");

        typeof(User).GetProperty("Id")?.SetValue(user, userId);

        if (existingAchievements is { Length: > 0 })
        {
            var json = System.Text.Json.JsonSerializer.Serialize(
                existingAchievements.Select(a => a.ToString()).ToList());
            user.SetAchievements(json);
        }

        return user;
    }

    private static Profile CreateProfile(
        Guid userId,
        bool hasPhoto,
        bool hasBio,
        bool hasPreferences,
        bool hasPrompts)
    {
        var profile = new Profile(
            userId,
            "TestUser",
            Gender.Male,
            new DateOnly(1995, 1, 1));

        if (hasBio)
        {
            profile.Update("TestUser", Gender.Male, "This is my bio");
        }

        if (hasPhoto)
        {
            profile.AddPhoto(new ProfilePhoto(profile.Id, Guid.NewGuid(), 0));
        }

        if (hasPreferences)
        {
            var pref = new ProfilePreference(
                profile.Id,
                [Gender.Female],
                25, 45, 50);
            profile.SetPreference(pref);
        }

        if (hasPrompts)
        {
            profile.SetPrompts([
                new ProfilePrompt(Guid.NewGuid(), "I love hiking on weekends", 0)
            ]);
        }

        return profile;
    }

    private ProfileCompletenessHandler CreateHandler()
    {
        return new ProfileCompletenessHandler(
            _userRepoMock.Object,
            _profileRepoMock.Object,
            _achievementRegistryMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);
    }
}
