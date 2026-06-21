using Dinder.Domain.Entities;
using Dinder.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace Dinder.UnitTests;

public class DinderDbContextConfigurationTests
{
    // Método helper: crea un DbContext en memoria para inspeccionar el modelo
    private static DbContextOptions<DinderDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<DinderDbContext>()
            .UseInMemoryDatabase("TestDb")
            .Options;
    }
    [Fact]
    public void Email_HasMaxLength256()
    {
        // Arrange: creamos el contexto y obtenemos la metadata del modelo
        using var context = new DinderDbContext(CreateOptions());
        var userEntity = context.Model.FindEntityType(typeof(User))!;
        // Act: buscamos la propiedad Email en el modelo
        var emailProperty = userEntity.FindProperty("Email")!;
        // Assert: OnModelCreating configuró HasMaxLength(256)
        Assert.Equal(256, emailProperty.GetMaxLength());
    }
    [Fact]
    public void Email_HasUniqueIndex()
    {
        // Arrange
        using var context = new DinderDbContext(CreateOptions());
        var userEntity = context.Model.FindEntityType(typeof(User))!;
        // Act: buscamos todos los índices sobre la propiedad Email
        var emailProperty = userEntity.FindProperty("Email")!;
        var emailIndex = userEntity.GetIndexes()
            .FirstOrDefault(i => i.Properties.Contains(emailProperty));
        // Assert: tiene que existir y ser único
        Assert.NotNull(emailIndex);
        Assert.True(emailIndex!.IsUnique);
    }
    [Fact]
    public void Id_IsPrimaryKey()
    {
        // Arrange
        using var context = new DinderDbContext(CreateOptions());
        var userEntity = context.Model.FindEntityType(typeof(User))!;
        // Act: buscamos la key primaria
        var primaryKey = userEntity.FindPrimaryKey()!;
        // Assert: la PK existe y contiene la propiedad Id
        Assert.NotNull(primaryKey);
        Assert.Contains(primaryKey.Properties, p => p.Name == "Id");
    }
    [Fact]
    public void PasswordHash_IsRequired()
    {
        // Arrange
        using var context = new DinderDbContext(CreateOptions());
        var userEntity = context.Model.FindEntityType(typeof(User))!;
        // Act: inspeccionamos la propiedad PasswordHash
        var passwordProperty = userEntity.FindProperty("PasswordHash")!;
        // Assert: IsRequired hace que la propiedad NO sea nullable
        Assert.False(passwordProperty.IsNullable);
    }
}
