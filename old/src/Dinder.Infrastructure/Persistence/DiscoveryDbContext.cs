using Dinder.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dinder.Infrastructure.Persistence;

public sealed class DiscoveryDbContext : DbContext
{
    public const string DiscoverySchema = "discovery";

    public DbSet<Swipe> Swipes => Set<Swipe>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DiscoveryDbContext(DbContextOptions<DiscoveryDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DiscoverySchema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DiscoveryDbContext).Assembly);
    }
}
