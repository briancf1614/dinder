using System.Security.Claims;
using Dinder.Application.Discovery.Commands;
using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Dinder.UnitTests;

public class SwipeCommandHandlerTests
{
    private readonly Mock<IDiscoveryRepository> _discoveryRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;

    public SwipeCommandHandlerTests()
    {
        _discoveryRepoMock = new Mock<IDiscoveryRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _mediatorMock = new Mock<IMediator>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
    }

    [Fact]
    public async Task FreeUser_25Swipes_Passes()
    {
        // Arrange
        SetupTier("Free");
        _discoveryRepoMock.Setup(r => r.GetDailySwipeCountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(24); // 25th swipe should pass

        var handler = CreateHandler();
        var command = new SwipeCommand(Guid.NewGuid(), Guid.NewGuid(), SwipeDirection.Right);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        _discoveryRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FreeUser_26thSwipe_ThrowsLimitReached()
    {
        // Arrange
        SetupTier("Free");
        _discoveryRepoMock.Setup(r => r.GetDailySwipeCountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(25); // Already at limit

        var handler = CreateHandler();
        var command = new SwipeCommand(Guid.NewGuid(), Guid.NewGuid(), SwipeDirection.Right);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.StartsWith("SWIPE_LIMIT_REACHED", ex.Message);
        Assert.Contains("Plus", ex.Message); // Upgrade tier included
    }

    [Fact]
    public async Task PlusUser_100Swipes_Passes()
    {
        // Arrange
        SetupTier("Plus");
        _discoveryRepoMock.Setup(r => r.GetDailySwipeCountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(99); // 100th swipe should pass

        var handler = CreateHandler();
        var command = new SwipeCommand(Guid.NewGuid(), Guid.NewGuid(), SwipeDirection.Left);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task PlusUser_101stSwipe_ThrowsLimitReachedWithPremiumUpgrade()
    {
        // Arrange
        SetupTier("Plus");
        _discoveryRepoMock.Setup(r => r.GetDailySwipeCountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(100); // At limit

        var handler = CreateHandler();
        var command = new SwipeCommand(Guid.NewGuid(), Guid.NewGuid(), SwipeDirection.Right);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Contains("Premium", ex.Message); // Upgrade tier
    }

    [Fact]
    public async Task PremiumUser_Unlimited_Passes()
    {
        // Arrange
        SetupTier("Premium");
        _discoveryRepoMock.Setup(r => r.GetDailySwipeCountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(500); // Well above any limit

        var handler = CreateHandler();
        var command = new SwipeCommand(Guid.NewGuid(), Guid.NewGuid(), SwipeDirection.Right);

        // Act — no exception
        var result = await handler.Handle(command, CancellationToken.None);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task NoHttpContext_DefaultsToFreeLimit()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        _discoveryRepoMock.Setup(r => r.GetDailySwipeCountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(25); // At Free limit

        var handler = CreateHandler();
        var command = new SwipeCommand(Guid.NewGuid(), Guid.NewGuid(), SwipeDirection.Right);

        // Act & Assert — defaults to Free, so 25 is at limit
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.StartsWith("SWIPE_LIMIT_REACHED", ex.Message);
    }

    // ── Bonus Swipe Tests (DI-7) ────────────────────────────────────────

    [Fact]
    public async Task FreeUser_7DayStreak_Allows30Swipes()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupTier("Free");
        SetupUserWithStreak(userId, 7);

        _discoveryRepoMock.Setup(r => r.GetDailySwipeCountAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(29); // 30th swipe with +5 bonus should pass

        var handler = CreateHandler();
        var command = new SwipeCommand(userId, Guid.NewGuid(), SwipeDirection.Right);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert — 25 base + 5 bonus = 30; 29 < 30, so passes
        Assert.NotNull(result);
        _discoveryRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FreeUser_7DayStreak_31stSwipe_ThrowsLimitReached()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupTier("Free");
        SetupUserWithStreak(userId, 7);

        _discoveryRepoMock.Setup(r => r.GetDailySwipeCountAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(30); // 25 base + 5 bonus = 30; at limit

        var handler = CreateHandler();
        var command = new SwipeCommand(userId, Guid.NewGuid(), SwipeDirection.Right);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.StartsWith("SWIPE_LIMIT_REACHED", ex.Message);
    }

    [Fact]
    public async Task FreeUser_30DayStreak_Allows40Swipes()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupTier("Free");
        SetupUserWithStreak(userId, 30);

        _discoveryRepoMock.Setup(r => r.GetDailySwipeCountAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(39); // 25 base + 15 bonus = 40; 39 < 40, passes

        var handler = CreateHandler();
        var command = new SwipeCommand(userId, Guid.NewGuid(), SwipeDirection.Right);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task FreeUser_45DayStreak_BonusCappedAt15()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupTier("Free");
        SetupUserWithStreak(userId, 45);

        _discoveryRepoMock.Setup(r => r.GetDailySwipeCountAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(40); // 25 base + 15 bonus (capped) = 40; at limit

        var handler = CreateHandler();
        var command = new SwipeCommand(userId, Guid.NewGuid(), SwipeDirection.Right);

        // Act & Assert — capped at +15, so 40 is at limit
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.StartsWith("SWIPE_LIMIT_REACHED", ex.Message);
    }

    [Fact]
    public async Task FreeUser_NoStreak_NoBonusSwipes()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupTier("Free");
        SetupUserWithStreak(userId, 0);

        _discoveryRepoMock.Setup(r => r.GetDailySwipeCountAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(25); // 25 base, no bonus; at limit

        var handler = CreateHandler();
        var command = new SwipeCommand(userId, Guid.NewGuid(), SwipeDirection.Right);

        // Act & Assert — no bonus, so 25 is at limit
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.StartsWith("SWIPE_LIMIT_REACHED", ex.Message);
    }

    [Fact]
    public async Task PremiumUser_WithStreak_UnlimitedStillPasses()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupTier("Premium");
        SetupUserWithStreak(userId, 30);

        _discoveryRepoMock.Setup(r => r.GetDailySwipeCountAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5000); // Way above any limit

        var handler = CreateHandler();
        var command = new SwipeCommand(userId, Guid.NewGuid(), SwipeDirection.Right);

        // Act — Premium has unlimited, even with bonus
        var result = await handler.Handle(command, CancellationToken.None);
        Assert.NotNull(result);
    }

    private SwipeCommandHandler CreateHandler()
    {
        return new SwipeCommandHandler(
            _discoveryRepoMock.Object,
            _userRepoMock.Object,
            _mediatorMock.Object,
            _httpContextAccessorMock.Object);
    }

    private void SetupUserWithStreak(Guid userId, int streak)
    {
        var user = new User(
            new Dinder.Domain.ValueObjects.Email("test@test.com"),
            "hash");
        typeof(User).GetProperty("Id")?.SetValue(user, userId);

        if (streak > 0)
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-(streak - 1));
            user.UpdateStreak(startDate, false); // Day 1
            for (int i = 1; i < streak; i++)
            {
                user.UpdateStreak(startDate.AddDays(i), true);
            }
        }

        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
    }

    private void SetupTier(string tier)
    {
        var claims = new List<Claim>
        {
            new("tier", tier),
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        };
        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);
        var context = new DefaultHttpContext { User = principal };
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);
    }
}
