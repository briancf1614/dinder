using Dinder.Application.Analytics.Handlers;
using Dinder.Domain.Events;
using Dinder.Domain.Interfaces;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dinder.IntegrationTests;

/// <summary>
/// Integration-style unit tests: verify fire-and-forget analytics handlers
/// are idempotent (multiple identical events produce consistent state).
/// </summary>
public class AnalyticsIdempotencyTests
{
    [Fact]
    public async Task TrackDAUHandler_DuplicateEvents_UpsertsNotInserts()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var analyticsRepoMock = new Mock<IAnalyticsRepository>();
        var count = 0;
        analyticsRepoMock.Setup(r => r.GetDailyActiveUserCountAsync(today, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => count);
        analyticsRepoMock.Setup(r => r.UpsertDailyActiveUserAsync(today, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<DateOnly, int, CancellationToken>((_, c, _) => count = c);

        var logger = NullLogger<TrackDAUHandler>.Instance;
        var handler = new TrackDAUHandler(analyticsRepoMock.Object, logger);

        // Act - fire the same event 3 times
        var notification = new UserLoggedInEvent(userId, DateTime.UtcNow);
        await handler.Handle(notification, CancellationToken.None);
        await handler.Handle(notification, CancellationToken.None);
        await handler.Handle(notification, CancellationToken.None);

        // Assert - upsert was called 3 times
        analyticsRepoMock.Verify(r => r.UpsertDailyActiveUserAsync(today, It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
        analyticsRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task TrackSwipeMetricsHandler_MultipleEvents_AggregatesCorrectly()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var existing = new SwipeMetricsSnapshot(today, 50, 25, 5);

        var analyticsRepoMock = new Mock<IAnalyticsRepository>();
        analyticsRepoMock.Setup(r => r.GetSwipeMetricsAsync(today, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var logger = NullLogger<TrackSwipeMetricsHandler>.Instance;
        var handler = new TrackSwipeMetricsHandler(analyticsRepoMock.Object, logger);

        // Act — 1 new right swipe
        var notification = new SwipeRecordedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Right");
        await handler.Handle(notification, CancellationToken.None);

        // Assert — values should be aggregated (50+1, 25+1)
        analyticsRepoMock.Verify(r => r.UpsertSwipeMetricsAsync(
            today, 51, 26, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        analyticsRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TrackSubscriptionHandler_ExistingSnapshot_IncrementsCount()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var analyticsRepoMock = new Mock<IAnalyticsRepository>();
        var snapshots = new Dictionary<string, int> { { "Plus", 10 } };
        analyticsRepoMock.Setup(r => r.GetSubscriptionSnapshotAsync(today, It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshots);

        var logger = NullLogger<TrackSubscriptionHandler>.Instance;
        var handler = new TrackSubscriptionHandler(analyticsRepoMock.Object, logger);

        // Act — Plus subscription activated
        var notification = new SubscriptionActivatedEvent(Guid.NewGuid(), Guid.NewGuid(), "Plus");
        await handler.Handle(notification, CancellationToken.None);

        // Assert — count should be 11 (10 + 1)
        analyticsRepoMock.Verify(r => r.UpsertSubscriptionSnapshotAsync(
            today, "Plus", 11, It.IsAny<CancellationToken>()), Times.Once);
        analyticsRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TrackSubscriptionHandler_NewTier_CreatesSnapshot()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var analyticsRepoMock = new Mock<IAnalyticsRepository>();
        analyticsRepoMock.Setup(r => r.GetSubscriptionSnapshotAsync(today, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int>());

        var logger = NullLogger<TrackSubscriptionHandler>.Instance;
        var handler = new TrackSubscriptionHandler(analyticsRepoMock.Object, logger);

        // Act — Premium subscription activated (first of the day)
        var notification = new SubscriptionActivatedEvent(Guid.NewGuid(), Guid.NewGuid(), "Premium");
        await handler.Handle(notification, CancellationToken.None);

        // Assert — count should be 1
        analyticsRepoMock.Verify(r => r.UpsertSubscriptionSnapshotAsync(
            today, "Premium", 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TrackDAUHandler_ExceptionDoesNotPropagate()
    {
        // Arrange
        var analyticsRepoMock = new Mock<IAnalyticsRepository>();
        analyticsRepoMock.Setup(r => r.GetDailyActiveUserCountAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB connection lost"));

        var logger = NullLogger<TrackDAUHandler>.Instance;
        var handler = new TrackDAUHandler(analyticsRepoMock.Object, logger);

        // Act — must NOT throw (fire-and-forget)
        var notification = new UserLoggedInEvent(Guid.NewGuid(), DateTime.UtcNow);
        await handler.Handle(notification, CancellationToken.None);

        // No exception = test passes
    }
}
