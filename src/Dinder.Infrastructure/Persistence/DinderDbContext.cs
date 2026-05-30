using Dinder.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dinder.Infrastructure.Persistence;

public sealed class DinderDbContext : DbContext
{
    public const string IdentitySchema = "identity";

    public DbSet<User> Users => Set<User>();
    public DbSet<UserExternalLogin> UserExternalLogins => Set<UserExternalLogin>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DinderDbContext(DbContextOptions<DinderDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(IdentitySchema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DinderDbContext).Assembly);
    }
}
