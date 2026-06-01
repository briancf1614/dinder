using Dinder.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dinder.Infrastructure.Persistence;

public sealed class MediaDbContext : DbContext
{
    public const string MediaSchema = "media";

    public DbSet<MediaFile> MediaFiles => Set<MediaFile>();

    public MediaDbContext(DbContextOptions<MediaDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(MediaSchema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MediaDbContext).Assembly);
    }
}
