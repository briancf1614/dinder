using Dinder.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dinder.IntegrationTests;

/// <summary>
/// WebApplicationFactory personalizado que reemplaza la DB de producción
/// por la del TestContainer (PostgreSQL real en Docker).
/// Corre las migraciones automáticamente al iniciar.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public CustomWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // 1. Eliminar el DbContext original (que usa la conn string de appsettings.json)
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<DinderDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            // 2. Reemplazar con la conn string del TestContainer
            services.AddDbContext<DinderDbContext>(options =>
                options.UseNpgsql(_connectionString));

            // 3. Aplicar migraciones para crear el schema
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DinderDbContext>();
            db.Database.EnsureDeleted();
            db.Database.Migrate();
        });
    }
}
