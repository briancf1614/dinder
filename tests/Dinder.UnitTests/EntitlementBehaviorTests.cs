using System.Security.Claims;
using Dinder.Application.Common.Attributes;
using Dinder.Application.Common.Behaviors;
using Dinder.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Dinder.UnitTests;

public class EntitlementBehaviorTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;

    public EntitlementBehaviorTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
    }

    private static Task<Unit> Next(CancellationToken ct)
    {
        return Task.FromResult(Unit.Value);
    }

    private static Task<Unit> NextThrowing(CancellationToken ct)
    {
        throw new InvalidOperationException("Should not reach handler.");
    }

    [Fact]
    public async Task NoAttribute_PassesThrough()
    {
        // Arrange
        var behavior = new EntitlementBehavior<TestCommand, Unit>(_httpContextAccessorMock.Object);
        var command = new TestCommand();
        var nextCalled = false;
        Task<Unit> NextTracked(CancellationToken ct)
        {
            nextCalled = true;
            return Task.FromResult(Unit.Value);
        }

        // Act
        await behavior.Handle(command, NextTracked, CancellationToken.None);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task PlusUser_AccessesPlusGatedEndpoint_Succeeds()
    {
        // Arrange
        var behavior = new EntitlementBehavior<TestCommand, Unit>(_httpContextAccessorMock.Object);
        SetupHttpContext(tier: "Plus", isAuthenticated: true);
        var command = new TestCommand();
        var nextCalled = false;
        Task<Unit> NextTracked(CancellationToken ct)
        {
            nextCalled = true;
            return Task.FromResult(Unit.Value);
        }

        // Act
        await behavior.Handle(command, NextTracked, CancellationToken.None);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task PremiumUser_AccessesPlusGatedEndpoint_Succeeds()
    {
        // Arrange
        var behavior = new EntitlementBehavior<TestCommand, Unit>(_httpContextAccessorMock.Object);
        SetupHttpContext(tier: "Premium", isAuthenticated: true);
        var command = new TestCommand();
        var nextCalled = false;
        Task<Unit> NextTracked(CancellationToken ct)
        {
            nextCalled = true;
            return Task.FromResult(Unit.Value);
        }

        // Act
        await behavior.Handle(command, NextTracked, CancellationToken.None);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task FreeUser_AccessesPlusGatedEndpoint_ThrowsUnauthorized()
    {
        // Arrange
        var behavior = new EntitlementBehavior<TestCommand, Unit>(_httpContextAccessorMock.Object);
        SetupHttpContext(tier: "Free", isAuthenticated: true);
        var command = new TestCommand();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => behavior.Handle(command, NextThrowing, CancellationToken.None));
    }

    [Fact]
    public async Task FreeUser_AccessesPremiumGatedEndpoint_ThrowsUnauthorized()
    {
        // Arrange
        var behavior = new EntitlementBehavior<PremiumGatedCommand, Unit>(_httpContextAccessorMock.Object);
        SetupHttpContext(tier: "Free", isAuthenticated: true);
        var command = new PremiumGatedCommand();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => behavior.Handle(command, NextThrowing, CancellationToken.None));
    }

    [Fact]
    public async Task UnauthenticatedUser_ThrowsUnauthorized()
    {
        // Arrange
        var behavior = new EntitlementBehavior<TestCommand, Unit>(_httpContextAccessorMock.Object);
        SetupHttpContext(tier: null, isAuthenticated: false);
        var command = new TestCommand();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => behavior.Handle(command, NextThrowing, CancellationToken.None));
    }

    [Fact]
    public async Task MissingTierClaim_ThrowsUnauthorized()
    {
        // Arrange
        var behavior = new EntitlementBehavior<TestCommand, Unit>(_httpContextAccessorMock.Object);
        SetupHttpContext(tier: null, isAuthenticated: true);
        var command = new TestCommand();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => behavior.Handle(command, NextThrowing, CancellationToken.None));
    }

    [Fact]
    public async Task NoHttpContext_AllowsThrough()
    {
        // Arrange (no HttpContext set up)
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        var behavior = new EntitlementBehavior<TestCommand, Unit>(_httpContextAccessorMock.Object);
        var command = new TestCommand();
        var nextCalled = false;
        Task<Unit> NextTracked(CancellationToken ct)
        {
            nextCalled = true;
            return Task.FromResult(Unit.Value);
        }

        // Act
        await behavior.Handle(command, NextTracked, CancellationToken.None);

        // Assert
        Assert.True(nextCalled);
    }

    private void SetupHttpContext(string? tier, bool isAuthenticated)
    {
        var claims = new List<Claim>();
        if (!string.IsNullOrEmpty(tier))
        {
            claims.Add(new Claim("tier", tier));
        }

        var identity = new ClaimsIdentity(claims, isAuthenticated ? "Bearer" : "");
        var principal = new ClaimsPrincipal(identity);
        var context = new DefaultHttpContext { User = principal };

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);
    }

    // ── Test commands ───────────────────────────────────────────────────

    [RequiresTier(SubscriptionTier.Plus)]
    public sealed class TestCommand : IRequest { }

    [RequiresTier(SubscriptionTier.Premium)]
    public sealed class PremiumGatedCommand : IRequest { }
}
