using Dinder.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dinder.Infrastructure.Persistence;

public sealed class NotificationDbContext : DbContext
{
    public const string NotificationSchema = "notification";

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<DeviceToken> DeviceTokens => Set<DeviceToken>();

    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(NotificationSchema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationDbContext).Assembly);
    }
}
