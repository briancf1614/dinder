using Dinder.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dinder.Infrastructure.Persistence;

public sealed class ProfileDbContext : DbContext
{
    public const string ProfileSchema = "profile";

    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<ProfilePhoto> ProfilePhotos => Set<ProfilePhoto>();
    public DbSet<ProfilePreference> ProfilePreferences => Set<ProfilePreference>();

    public ProfileDbContext(DbContextOptions<ProfileDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(ProfileSchema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProfileDbContext).Assembly);
    }
}
