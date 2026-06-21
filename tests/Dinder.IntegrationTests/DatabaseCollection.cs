using Testcontainers.PostgreSql;
using Xunit;

namespace Dinder.IntegrationTests;

// Collection Fixture: un solo contenedor PostgreSQL para todos los tests de integración
[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture> { }

public class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;

    public DatabaseFixture()
    {
        _container = new PostgreSqlBuilder()
            .WithDatabase("dinder_test")
            .WithUsername("dinder")
            .WithPassword("dinder123")
            .WithImage("postgres:17-alpine")
            .WithCleanUp(true)
            .Build();
    }

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
