using Dinder.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dinder.Infrastructure.Persistence;

public sealed class ModerationDbContext : DbContext
{
    public const string ModerationSchema = "moderation";

    public DbSet<Report> Reports => Set<Report>();
    public DbSet<Block> Blocks => Set<Block>();
    public DbSet<PhotoReview> PhotoReviews => Set<PhotoReview>();

    public ModerationDbContext(DbContextOptions<ModerationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(ModerationSchema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ModerationDbContext).Assembly);
    }
}
