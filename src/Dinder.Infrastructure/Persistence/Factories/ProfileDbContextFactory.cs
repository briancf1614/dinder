using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dinder.Infrastructure.Persistence.Factories;

public sealed class ProfileDbContextFactory : IDesignTimeDbContextFactory<ProfileDbContext>
{
    public ProfileDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ProfileDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=dinder;Username=postgres;Password=postgres",
            npgsqlOptions => npgsqlOptions.UseNetTopologySuite());

        return new ProfileDbContext(optionsBuilder.Options);
    }
}
