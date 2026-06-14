using Dinder.Application.Discovery.Commands;
using Dinder.Application.Discovery.Queries;
using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Domain.Interfaces;
using Moq;
using Xunit;

namespace Dinder.UnitTests;

public class PremiumFeatureTests
{
    [Fact]
    public async Task UndoSwipe_RemovesLastSwipe()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var swipedId = Guid.NewGuid();
        var swipe = new Swipe(userId, swipedId, SwipeDirection.Left);

        var repoMock = new Mock<IDiscoveryRepository>();
        repoMock.Setup(r => r.GetLastSwipeAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(swipe);

        var handler = new UndoSwipeCommandHandler(repoMock.Object);
        var command = new UndoSwipeCommand(userId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        repoMock.Verify(r => r.RemoveSwipe(swipe), Times.Once);
        repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UndoSwipe_NoSwipes_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var repoMock = new Mock<IDiscoveryRepository>();
        repoMock.Setup(r => r.GetLastSwipeAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Swipe?)null);

        var handler = new UndoSwipeCommandHandler(repoMock.Object);
        var command = new UndoSwipeCommand(userId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("No swipes to undo.", result.Message);
        repoMock.Verify(r => r.RemoveSwipe(It.IsAny<Swipe>()), Times.Never);
    }

    [Fact]
    public async Task GetLikes_ReturnsUsersWhoLiked()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var likerId = Guid.NewGuid();
        var likes = new List<Swipe>
        {
            new(likerId, userId, SwipeDirection.Right)
        };

        var repoMock = new Mock<IDiscoveryRepository>();
        repoMock.Setup(r => r.GetLikesForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(likes);

        var handler = new GetLikesQueryHandler(repoMock.Object);
        var query = new GetLikesQuery(userId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(likerId, result[0].UserId);
    }

    [Fact]
    public async Task Boost_Success()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = new Profile(userId, "Test", Gender.Male, new DateOnly(2000, 1, 1));

        var repoMock = new Mock<IProfileRepository>();
        repoMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var handler = new BoostCommandHandler(repoMock.Object);
        var command = new BoostCommand(userId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.BoostedAt);
        repoMock.Verify(r => r.Update(profile), Times.Once);
        repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Boost_AlreadyBoostedThisMonth_Fails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = new Profile(userId, "Test", Gender.Female, new DateOnly(2000, 6, 15));
        profile.Boost(); // First boost succeeds
        Assert.True(profile.BoostedAt.HasValue);

        var repoMock = new Mock<IProfileRepository>();
        repoMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var handler = new BoostCommandHandler(repoMock.Object);
        var command = new BoostCommand(userId);

        // Act — second boost in same month
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("already boosted", result.Message);
        repoMock.Verify(r => r.Update(It.IsAny<Profile>()), Times.Never);
    }

    [Fact]
    public async Task Boost_ProfileNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var repoMock = new Mock<IProfileRepository>();
        repoMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Profile?)null);

        var handler = new BoostCommandHandler(repoMock.Object);
        var command = new BoostCommand(userId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Profile not found.", result.Message);
    }
}
