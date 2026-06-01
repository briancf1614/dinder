using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dinder.Infrastructure.Persistence.Factories;

public sealed class ModerationDbContextFactory : IDesignTimeDbContextFactory<ModerationDbContext>
{
    public ModerationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ModerationDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=dinder;Username=postgres;Password=postgres",
            npgsqlOptions => npgsqlOptions.UseNetTopologySuite());

        return new ModerationDbContext(optionsBuilder.Options);
    }
}
