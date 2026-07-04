using Dinder.Application.Common.Commands.Auth.Login;
using Dinder.Application.Common.Interfaces;
using Dinder.Domain.Entities;
using Dinder.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Dinder.UnitTests.Auth;

public class LoginCommandHandlerTests
{
    private DinderDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<DinderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DinderDbContext(options);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsAuthResponseWithTokens()
    {
        // ── Arrange: creamos un usuario con password hasheada ──
        using var dbContext = CreateInMemoryContext();
        var plainPassword = "CorrectPass1!";
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(plainPassword);

        dbContext.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "login@test.com",
            PasswordHash = hashedPassword,
            CreatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var mockTokenService = new Mock<ITokenService>();
        mockTokenService.Setup(t => t.GenerateToken(It.IsAny<User>()))
            .Returns("fake-jwt");
        mockTokenService.Setup(t => t.GenerateRefreshToken())
            .Returns("fake-refresh");

        var handler = new LoginCommandHandler(dbContext, mockTokenService.Object);
        var command = new LoginCommand("login@test.com", plainPassword);

        // ── Act ──
        var result = await handler.Handle(command, CancellationToken.None);

        // ── Assert ──
        Assert.NotNull(result);
        Assert.Equal("fake-jwt", result.Token);
        Assert.Equal("fake-refresh", result.RefreshToken);

        // Verificamos que el refresh token se guardó en el usuario
        var updatedUser = await dbContext.Users.FirstAsync(u => u.Email == "login@test.com");
        Assert.Equal("fake-refresh", updatedUser.RefreshToken);
        Assert.NotNull(updatedUser.RefreshTokenExpiry);
    }

    [Fact]
    public async Task Handle_WrongPassword_ThrowsUnauthorizedAccessException()
    {
        // ── Arrange: usuario existe pero la password no coincide ──
        using var dbContext = CreateInMemoryContext();
        var correctPassword = "RealPass1!";
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(correctPassword);

        dbContext.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "locked@test.com",
            PasswordHash = hashedPassword,
            CreatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var mockTokenService = new Mock<ITokenService>();
        var handler = new LoginCommandHandler(dbContext, mockTokenService.Object);

        // Mandamos una password DISTINTA a la real
        var command = new LoginCommand("locked@test.com", "WrongPassword1!");

        // ── Act & Assert ──
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Contains("incorrectos", ex.Message.ToLower());
    }
}
