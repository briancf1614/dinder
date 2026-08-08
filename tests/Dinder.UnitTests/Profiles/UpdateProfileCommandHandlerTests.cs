using Dinder.Application.Common.Commands.Profiles.UpdateProfile;
using Dinder.Application.Common.Interfaces;
using Dinder.Application.Common.Models;
using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Dinder.UnitTests.Profiles;

public class UpdateProfileCommandHandlerTests
{
    private DinderDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<DinderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DinderDbContext(options);
    }

    private Mock<IHttpContextAccessor> CreateHttpContextMock(string email)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, email)
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(x => x.User).Returns(principal);

        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);
        return accessorMock;
    }

    /// <summary>
    /// Happy path: PUT with all 4 profile fields → user is updated → 7-field MeResponse returned.
    /// </summary>
    [Fact]
    public async Task Handle_ValidProfile_UpdatesUserAndReturns7FieldMeResponse()
    {
        // ── Arrange ──
        using var db = CreateInMemoryContext();
        var userId = Guid.NewGuid();
        var email = "profile-test@example.com";
        db.Users.Add(new User
        {
            Id = userId,
            Email = email,
            PasswordHash = "hashed",
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            DisplayName = null,
            Bio = null,
            BirthDate = null,
            Gender = null
        });
        await db.SaveChangesAsync();

        var mockHttp = CreateHttpContextMock(email);
        var handler = new UpdateProfileCommandHandler(db, mockHttp.Object);

        var command = new UpdateProfileCommand(
            "TestUser",
            "Hello, I love hiking!",
            new DateOnly(1995, 6, 15),
            Gender.Male
        );

        // ── Act ──
        var result = await handler.Handle(command, CancellationToken.None);

        // ── Assert: 7-field MeResponse ──
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal(email, result.Email);
        Assert.Equal(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), result.CreatedAt);
        Assert.Equal("TestUser", result.DisplayName);
        Assert.Equal("Hello, I love hiking!", result.Bio);
        Assert.Equal(new DateOnly(1995, 6, 15), result.BirthDate);
        Assert.Equal(Gender.Male, result.Gender);

        // ── Assert: user entity updated in DB ──
        var updated = await db.Users.FirstAsync(u => u.Id == userId);
        Assert.Equal("TestUser", updated.DisplayName);
        Assert.Equal("Hello, I love hiking!", updated.Bio);
        Assert.Equal(new DateOnly(1995, 6, 15), updated.BirthDate);
        Assert.Equal(Gender.Male, updated.Gender);
    }

    /// <summary>
    /// User not found: email from JWT doesn't match any user → UnauthorizedAccessException.
    /// </summary>
    [Fact]
    public async Task Handle_UserNotFound_ThrowsUnauthorizedAccessException()
    {
        // ── Arrange ──
        using var db = CreateInMemoryContext();
        var mockHttp = CreateHttpContextMock("nonexistent@example.com");
        var handler = new UpdateProfileCommandHandler(db, mockHttp.Object);

        var command = new UpdateProfileCommand(
            "Ghost",
            null,
            null,
            null
        );

        // ── Act & Assert ──
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Contains("Usuario no encontrado", ex.Message);
    }

    /// <summary>
    /// Missing email claim: JWT has no email → UnauthorizedAccessException.
    /// </summary>
    [Fact]
    public async Task Handle_NoEmailClaim_ThrowsUnauthorizedAccessException()
    {
        // ── Arrange ──
        using var db = CreateInMemoryContext();
        var identity = new ClaimsIdentity(); // no claims
        var principal = new ClaimsPrincipal(identity);

        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(x => x.User).Returns(principal);

        var mockHttp = new Mock<IHttpContextAccessor>();
        mockHttp.Setup(x => x.HttpContext).Returns(httpContextMock.Object);

        var handler = new UpdateProfileCommandHandler(db, mockHttp.Object);

        var command = new UpdateProfileCommand(
            "NoEmail",
            null,
            null,
            null
        );

        // ── Act & Assert ──
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));
        Assert.Contains("no autenticado", ex.Message);
    }
}
