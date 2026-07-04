using Dinder.Application.Common.Commands.Auth.Register;
using Dinder.Application.Common.Interfaces;
using Dinder.Application.Common.Models;
using Dinder.Domain.Entities;
using Dinder.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Dinder.UnitTests.Auth;

public class RegisterCommandHandlerTests
{
    /// <summary>
    /// Crea un DbContext en memoria (InMemory) que implementa IApplicationDbContext.
    /// Cada test tiene su propia base de datos aislada.
    /// </summary>
    private DinderDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<DinderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DinderDbContext(options);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsAuthResponseWithTokens()
    {
        // ── Arrange ──
        using var dbContext = CreateInMemoryContext();
        var mockTokenService = new Mock<ITokenService>();
        mockTokenService.Setup(t => t.GenerateToken(It.IsAny<User>()))
            .Returns("fake-jwt-token");
        mockTokenService.Setup(t => t.GenerateRefreshToken())
            .Returns("fake-refresh-token");

        var handler = new RegisterCommandHandler(dbContext, mockTokenService.Object);
        var command = new RegisterCommand("test@example.com", "Password123!");

        // ── Act ──
        var result = await handler.Handle(command, CancellationToken.None);

        // ── Assert ──
        Assert.NotNull(result);
        Assert.Equal("fake-jwt-token", result.Token);
        Assert.Equal("fake-refresh-token", result.RefreshToken);

        // Verificamos que el usuario se guardó en la DB
        var savedUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
        Assert.NotNull(savedUser);
        Assert.NotEmpty(savedUser!.PasswordHash);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsValidationException()
    {
        // ── Arrange: ya existe un usuario con ese email ──
        using var dbContext = CreateInMemoryContext();
        dbContext.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "dup@example.com",
            PasswordHash = "existing-hash",
            CreatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var mockTokenService = new Mock<ITokenService>();
        var handler = new RegisterCommandHandler(dbContext, mockTokenService.Object);
        var command = new RegisterCommand("dup@example.com", "Password123!");

        // ── Act & Assert ──
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Contains("Email", ex.Message); // El mensaje menciona el email
    }

    [Fact]
    public async Task Handle_SavesHashedPassword_NotPlaintext()
    {
        // ── Arrange ──
        using var dbContext = CreateInMemoryContext();
        var mockTokenService = new Mock<ITokenService>();
        mockTokenService.Setup(t => t.GenerateToken(It.IsAny<User>()))
            .Returns("token");
        mockTokenService.Setup(t => t.GenerateRefreshToken())
            .Returns("refresh");

        var handler = new RegisterCommandHandler(dbContext, mockTokenService.Object);
        var plainPassword = "MySecret123!";
        var command = new RegisterCommand("hash-test@example.com", plainPassword);

        // ── Act ──
        await handler.Handle(command, CancellationToken.None);

        // ── Assert: la password guardada NO es el texto plano ──
        var savedUser = await dbContext.Users
            .FirstAsync(u => u.Email == "hash-test@example.com");
        Assert.NotEqual(plainPassword, savedUser.PasswordHash);
        // Verificamos que ES un hash de BCrypt (empieza con $2a$, $2b$ o $2y$)
        Assert.StartsWith("$2", savedUser.PasswordHash);
    }
}
