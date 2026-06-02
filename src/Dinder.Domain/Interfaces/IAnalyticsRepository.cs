using Dinder.Domain.Entities;

namespace Dinder.Domain.Interfaces;

public interface IAnalyticsRepository
{
    // ── Daily Active Users ────────────────────────────────────────────

    Task UpsertDailyActiveUserAsync(DateOnly date, int userCount, CancellationToken cancellationToken = default);
    Task<int> GetDailyActiveUserCountAsync(DateOnly date, CancellationToken cancellationToken = default);
    Task<Dictionary<DateOnly, int>> GetDailyActiveUsersAsync(int days, CancellationToken cancellationToken = default);

    // ── Subscription Snapshots ────────────────────────────────────────

    Task UpsertSubscriptionSnapshotAsync(DateOnly date, string tier, int count, CancellationToken cancellationToken = default);
    Task<Dictionary<string, int>> GetSubscriptionSnapshotAsync(DateOnly date, CancellationToken cancellationToken = default);

    // ── Swipe Metrics ─────────────────────────────────────────────────

    Task UpsertSwipeMetricsAsync(DateOnly date, int totalSwipes, int totalRightSwipes, int totalMatches, CancellationToken cancellationToken = default);
    Task<SwipeMetricsSnapshot?> GetSwipeMetricsAsync(DateOnly date, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed record SwipeMetricsSnapshot(DateOnly Date, int TotalSwipes, int TotalRightSwipes, int TotalMatches);
