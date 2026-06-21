using Dinder.Domain.Entities;
using Xunit;
namespace Dinder.UnitTests;

public class UserEntityTests
{
    [Fact]
    public void Constructor_WhenCalled_HasDefaultValues()
    {
        // Arrange
        var user = new User();
        // Assert: una entidad recién creada tiene defaults de C#
        Assert.Equal(Guid.Empty, user.Id);
        Assert.Equal(string.Empty, user.Email);
        Assert.Equal(string.Empty, user.PasswordHash);
        Assert.Equal(DateTime.MinValue, user.CreatedAt);
    }
    [Fact]
    public void Properties_WhenSet_ReturnAssignedValues()
    {
        // Arrange
        var id = Guid.NewGuid();
        var createdAt = new DateTime(2026, 6, 21, 12, 0, 0, DateTimeKind.Utc);
        var user = new User();
        // Act
        user.Id = id;
        user.Email = "test@example.com";
        user.PasswordHash = "hash123";
        user.CreatedAt = createdAt;
        // Assert
        Assert.Equal(id, user.Id);
        Assert.Equal("test@example.com", user.Email);
        Assert.Equal("hash123", user.PasswordHash);
        Assert.Equal(createdAt, user.CreatedAt);
    }
}
