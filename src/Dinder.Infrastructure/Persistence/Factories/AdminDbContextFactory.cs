using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dinder.Infrastructure.Persistence.Factories;

public sealed class AdminDbContextFactory : IDesignTimeDbContextFactory<AdminDbContext>
{
    public AdminDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AdminDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=dinder;Username=postgres;Password=postgres",
            npgsqlOptions => npgsqlOptions.UseNetTopologySuite());

        return new AdminDbContext(optionsBuilder.Options);
    }
}
