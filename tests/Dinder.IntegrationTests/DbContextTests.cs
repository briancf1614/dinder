using Dinder.Domain.Entities;
using Dinder.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Dinder.IntegrationTests;

[Collection("Database")]
public class DbContextTests
{
    private readonly DatabaseFixture _fixture;

    public DbContextTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private DinderDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DinderDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        return new DinderDbContext(options);
    }

    [Fact]
    public void EnsureCreated_Succeeds()
    {
        // Arrange: limpiamos cualquier DB previa y creamos contexto fresco
        using var context = CreateContext();
        context.Database.EnsureDeleted();
        // Act: EnsureCreated crea la DB con el modelo (sin migraciones)
        var created = context.Database.EnsureCreated();
        // Assert: devuelve true porque la DB no existía
        Assert.True(created);
    }

    [Fact]
    public void SaveAndRetrieve_User_RoundTrip()
    {
        // Arrange
        using var context = CreateContext();
        context.Database.EnsureCreated();

        var id = Guid.NewGuid();
        var createdAt = new DateTime(2026, 6, 21, 12, 0, 0, DateTimeKind.Utc);
        var user = new User
        {
            Id = id,
            Email = "roundtrip@test.com",
            PasswordHash = "hash123",
            CreatedAt = createdAt
        };

        // Act: guardamos
        context.Users.Add(user);
        context.SaveChanges();

        // Limpiar el tracker para leer de la DB, no de la memoria
        context.ChangeTracker.Clear();

        // Recuperamos
        var retrieved = context.Users.Find(id);

        // Assert: idéntico al original
        Assert.NotNull(retrieved);
        Assert.Equal("roundtrip@test.com", retrieved!.Email);
        Assert.Equal("hash123", retrieved.PasswordHash);
        Assert.Equal(createdAt, retrieved.CreatedAt);
    }

    [Fact]
    public void DuplicateEmail_ThrowsDbUpdateException()
    {
        // Arrange
        using var context = CreateContext();
        context.Database.EnsureCreated();

        context.Users.Add(new User { Email = "dup@test.com", PasswordHash = "hash" });
        context.SaveChanges();

        // Act: intentamos insertar otro con el mismo email
        context.Users.Add(new User { Email = "dup@test.com", PasswordHash = "hash" });

        // Assert: la DB rechaza el duplicado
        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
