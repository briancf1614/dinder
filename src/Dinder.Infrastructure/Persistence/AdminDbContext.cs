using Dinder.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dinder.Infrastructure.Persistence;

public sealed class AdminDbContext : DbContext
{
    public const string AdminSchema = "admin";

    public DbSet<AdminAuditLog> AuditLogs => Set<AdminAuditLog>();

    public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(AdminSchema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AdminDbContext).Assembly);
    }
}
