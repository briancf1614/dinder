using Dinder.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Dinder.IntegrationTests;

[Collection("Database")]
public class MigrationTests
{
    private readonly DatabaseFixture _fixture;

    public MigrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Migrate_OnFreshDatabase_Succeeds()
    {
        // Arrange: contexto con la connection string del container
        var options = new DbContextOptionsBuilder<DinderDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;

        using var context = new DinderDbContext(options);

        // Aseguramos base limpia antes de migrar
        context.Database.EnsureDeleted();

        // Act: aplica la migración InitialCreate
        context.Database.Migrate();

        // Assert: la tabla Users existe después de migrar
        var tableExists = context.Database
            .SqlQueryRaw<bool>(
                "SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'Users')")
            .AsEnumerable()
            .First();

        Assert.True(tableExists);
    }
}
