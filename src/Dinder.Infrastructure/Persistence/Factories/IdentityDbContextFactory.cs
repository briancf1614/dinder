using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dinder.Infrastructure.Persistence.Factories;

public sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<DinderDbContext>
{
    public DinderDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DinderDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=dinder;Username=postgres;Password=postgres",
            npgsqlOptions => npgsqlOptions.UseNetTopologySuite());

        return new DinderDbContext(optionsBuilder.Options);
    }
}
