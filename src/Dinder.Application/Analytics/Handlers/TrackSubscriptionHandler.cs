using Dinder.Domain.Events;
using Dinder.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Dinder.Application.Analytics.Handlers;

/// <summary>
/// Fire-and-forget handler: records subscription activations by tier for daily snapshots.
/// Tracks how many users activated each tier per day. Uses upsert for idempotency.
/// </summary>
public sealed class TrackSubscriptionHandler : INotificationHandler<SubscriptionActivatedEvent>
{
    private readonly IAnalyticsRepository _analyticsRepository;
    private readonly ILogger<TrackSubscriptionHandler> _logger;

    public TrackSubscriptionHandler(IAnalyticsRepository analyticsRepository, ILogger<TrackSubscriptionHandler> logger)
    {
        _analyticsRepository = analyticsRepository;
        _logger = logger;
    }

    public async Task Handle(SubscriptionActivatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            // Get current count for this tier today
            var snapshots = await _analyticsRepository.GetSubscriptionSnapshotAsync(today, cancellationToken);
            var tier = notification.Tier;
            var currentCount = snapshots.TryGetValue(tier, out var count) ? count : 0;

            await _analyticsRepository.UpsertSubscriptionSnapshotAsync(
                today, tier, currentCount + 1, cancellationToken);
            await _analyticsRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Subscription snapshot updated for {Date}: Tier={Tier}, Count={Count}",
                today, tier, currentCount + 1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track subscription activation for user {UserId}", notification.UserId);
        }
    }
}
