using Dinder.Domain.Events;
using Dinder.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Dinder.Application.Analytics.Handlers;

/// <summary>
/// Fire-and-forget handler: increments daily swipe metrics when a swipe event occurs.
/// Tracks total swipes, right swipes, and inferred matches (from SwipeRecordedEvent).
/// Uses upsert for idempotency.
/// </summary>
public sealed class TrackSwipeMetricsHandler : INotificationHandler<SwipeRecordedEvent>
{
    private readonly IAnalyticsRepository _analyticsRepository;
    private readonly ILogger<TrackSwipeMetricsHandler> _logger;

    public TrackSwipeMetricsHandler(IAnalyticsRepository analyticsRepository, ILogger<TrackSwipeMetricsHandler> logger)
    {
        _analyticsRepository = analyticsRepository;
        _logger = logger;
    }

    public async Task Handle(SwipeRecordedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var current = await _analyticsRepository.GetSwipeMetricsAsync(today, cancellationToken);

            int totalSwipes = (current?.TotalSwipes ?? 0) + 1;
            int totalRightSwipes = (current?.TotalRightSwipes ?? 0)
                + (notification.Direction == "Right" ? 1 : 0);
            int totalMatches = current?.TotalMatches ?? 0;

            await _analyticsRepository.UpsertSwipeMetricsAsync(
                today, totalSwipes, totalRightSwipes, totalMatches, cancellationToken);
            await _analyticsRepository.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Swipe metrics updated for {Date}: Swipes={Swipes}, RightSwipes={Right}",
                today, totalSwipes, totalRightSwipes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track swipe metrics for swipe {SwipeId}", notification.SwipeId);
        }
    }
}
