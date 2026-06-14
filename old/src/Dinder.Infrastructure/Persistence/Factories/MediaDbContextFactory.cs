using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dinder.Infrastructure.Persistence.Factories;

public sealed class MediaDbContextFactory : IDesignTimeDbContextFactory<MediaDbContext>
{
    public MediaDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MediaDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=dinder;Username=postgres;Password=postgres",
            npgsqlOptions => npgsqlOptions.UseNetTopologySuite());

        return new MediaDbContext(optionsBuilder.Options);
    }
}
