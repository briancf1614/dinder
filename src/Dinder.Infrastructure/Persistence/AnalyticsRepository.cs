using Dinder.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Dinder.Infrastructure.Persistence;

public sealed class AnalyticsRepository : IAnalyticsRepository
{
    private readonly AnalyticsDbContext _context;

    public AnalyticsRepository(AnalyticsDbContext context)
    {
        _context = context;
    }

    // ── Daily Active Users ────────────────────────────────────────────

    public async Task UpsertDailyActiveUserAsync(DateOnly date, int userCount, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Set<DailyActiveUser>()
            .FirstOrDefaultAsync(x => x.Date == date, cancellationToken);

        if (existing is not null)
        {
            existing.UserCount = userCount;
        }
        else
        {
            _context.Set<DailyActiveUser>().Add(new DailyActiveUser { Date = date, UserCount = userCount });
        }
    }

    public async Task<int> GetDailyActiveUserCountAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var dau = await _context.Set<DailyActiveUser>()
            .FirstOrDefaultAsync(x => x.Date == date, cancellationToken);
        return dau?.UserCount ?? 0;
    }

    public async Task<Dictionary<DateOnly, int>> GetDailyActiveUsersAsync(int days, CancellationToken cancellationToken = default)
    {
        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-days));
        return await _context.Set<DailyActiveUser>()
            .Where(x => x.Date >= cutoff)
            .OrderByDescending(x => x.Date)
            .ToDictionaryAsync(x => x.Date, x => x.UserCount, cancellationToken);
    }

    // ── Subscription Snapshots ────────────────────────────────────────

    public async Task UpsertSubscriptionSnapshotAsync(DateOnly date, string tier, int count, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Set<SubscriptionSnapshot>()
            .FirstOrDefaultAsync(x => x.Date == date && x.Tier == tier, cancellationToken);

        if (existing is not null)
        {
            existing.Count = count;
        }
        else
        {
            _context.Set<SubscriptionSnapshot>().Add(new SubscriptionSnapshot { Date = date, Tier = tier, Count = count });
        }
    }

    public async Task<Dictionary<string, int>> GetSubscriptionSnapshotAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        return await _context.Set<SubscriptionSnapshot>()
            .Where(x => x.Date == date)
            .ToDictionaryAsync(x => x.Tier, x => x.Count, cancellationToken);
    }

    // ── Swipe Metrics ─────────────────────────────────────────────────

    public async Task UpsertSwipeMetricsAsync(DateOnly date, int totalSwipes, int totalRightSwipes, int totalMatches, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Set<SwipeMetric>()
            .FirstOrDefaultAsync(x => x.Date == date, cancellationToken);

        if (existing is not null)
        {
            existing.TotalSwipes = totalSwipes;
            existing.TotalRightSwipes = totalRightSwipes;
            existing.TotalMatches = totalMatches;
        }
        else
        {
            _context.Set<SwipeMetric>().Add(new SwipeMetric
            {
                Date = date,
                TotalSwipes = totalSwipes,
                TotalRightSwipes = totalRightSwipes,
                TotalMatches = totalMatches
            });
        }
    }

    public async Task<SwipeMetricsSnapshot?> GetSwipeMetricsAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var metric = await _context.Set<SwipeMetric>()
            .FirstOrDefaultAsync(x => x.Date == date, cancellationToken);

        return metric is null
            ? null
            : new SwipeMetricsSnapshot(metric.Date, metric.TotalSwipes, metric.TotalRightSwipes, metric.TotalMatches);
    }

    // ── Save ──────────────────────────────────────────────────────────

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
