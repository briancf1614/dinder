using Microsoft.EntityFrameworkCore;

namespace Dinder.Infrastructure.Persistence;

public sealed class AnalyticsDbContext : DbContext
{
    public const string AnalyticsSchema = "analytics";

    public AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(AnalyticsSchema);

        modelBuilder.Entity<DailyActiveUser>(entity =>
        {
            entity.ToTable("daily_active_users");
            entity.HasKey(x => x.Date);
            entity.Property(x => x.UserCount).IsRequired();
        });

        modelBuilder.Entity<SubscriptionSnapshot>(entity =>
        {
            entity.ToTable("subscription_snapshots");
            entity.HasKey(x => new { x.Date, x.Tier });
            entity.Property(x => x.Count).IsRequired();
        });

        modelBuilder.Entity<SwipeMetric>(entity =>
        {
            entity.ToTable("swipe_metrics");
            entity.HasKey(x => x.Date);
            entity.Property(x => x.TotalSwipes).IsRequired();
            entity.Property(x => x.TotalRightSwipes).IsRequired();
            entity.Property(x => x.TotalMatches).IsRequired();
        });
    }
}

// ── Analytics Entity Models (owned by AnalyticsDbContext) ──────────────

public sealed class DailyActiveUser
{
    public DateOnly Date { get; set; }
    public int UserCount { get; set; }
}

public sealed class SubscriptionSnapshot
{
    public DateOnly Date { get; set; }
    public string Tier { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class SwipeMetric
{
    public DateOnly Date { get; set; }
    public int TotalSwipes { get; set; }
    public int TotalRightSwipes { get; set; }
    public int TotalMatches { get; set; }
}
