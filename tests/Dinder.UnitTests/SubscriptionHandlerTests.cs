using Dinder.Application.Common.Interfaces;
using Dinder.Application.Common.Models;
using Dinder.Application.Subscription.Commands;
using Dinder.Application.Subscription.Queries;
using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Domain.Interfaces;
using Moq;
using Xunit;
using SubscriptionEntity = Dinder.Domain.Entities.Subscription;

namespace Dinder.UnitTests;

public class SubscriptionHandlerTests
{
    private readonly Mock<ISubscriptionRepository> _subRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IStripeService> _stripeServiceMock;
    private readonly Mock<IStripePriceResolver> _priceResolverMock;

    public SubscriptionHandlerTests()
    {
        _subRepoMock = new Mock<ISubscriptionRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _stripeServiceMock = new Mock<IStripeService>();
        _priceResolverMock = new Mock<IStripePriceResolver>();
    }

    #region CreateCheckoutSession

    [Fact]
    public async Task CreateCheckoutSession_ReturnsSessionUrl()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var tier = SubscriptionTier.Plus;
        var priceId = "price_plus";
        var sessionUrl = "https://checkout.stripe.com/session/cs_123";

        _priceResolverMock.Setup(r => r.GetPriceId(tier)).Returns(priceId);
        _subRepoMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionEntity?)null);
        _stripeServiceMock.Setup(s => s.CreateCheckoutSessionAsync(
                userId, email, priceId, tier, It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionUrl);

        var handler = new CreateCheckoutSessionCommandHandler(
            _subRepoMock.Object, _stripeServiceMock.Object, _priceResolverMock.Object);
        var command = new CreateCheckoutSessionCommand(userId, email, tier);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(sessionUrl, result.SessionUrl);
    }

    [Fact]
    public async Task CreateCheckoutSession_AlreadySubscribedSameTier_Throws()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tier = SubscriptionTier.Plus;

        var existing = new SubscriptionEntity(userId, "sub_456", tier, DateTime.UtcNow.AddMonths(1));
        _subRepoMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var handler = new CreateCheckoutSessionCommandHandler(
            _subRepoMock.Object, _stripeServiceMock.Object, _priceResolverMock.Object);
        var command = new CreateCheckoutSessionCommand(userId, "test@example.com", tier);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    #endregion

    #region CreatePortalSession

    [Fact]
    public async Task CreatePortalSession_ReturnsPortalUrl()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var customerId = "cus_123";
        var portalUrl = "https://billing.stripe.com/session/ps_123";

        var user = new User(new Dinder.Domain.ValueObjects.Email("test@example.com"), "hash");
        user.SetStripeCustomerId(customerId);

        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _stripeServiceMock.Setup(s => s.CreatePortalSessionAsync(
                customerId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(portalUrl);

        var handler = new CreatePortalSessionCommandHandler(_userRepoMock.Object, _stripeServiceMock.Object);
        var command = new CreatePortalSessionCommand(userId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(portalUrl, result.PortalUrl);
    }

    [Fact]
    public async Task CreatePortalSession_NoCustomerId_Throws()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User(new Dinder.Domain.ValueObjects.Email("test@example.com"), "hash");

        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = new CreatePortalSessionCommandHandler(_userRepoMock.Object, _stripeServiceMock.Object);
        var command = new CreatePortalSessionCommand(userId);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    #endregion

    #region GetSubscriptionStatus

    [Fact]
    public async Task GetSubscriptionStatus_ReturnsStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User(new Dinder.Domain.ValueObjects.Email("test@example.com"), "hash");
        user.SetTier(SubscriptionTier.Plus);

        var subscription = new SubscriptionEntity(userId, "sub_123", SubscriptionTier.Plus, DateTime.UtcNow.AddMonths(1));

        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subRepoMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var handler = new GetSubscriptionStatusQueryHandler(_subRepoMock.Object, _userRepoMock.Object);
        var query = new GetSubscriptionStatusQuery(userId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SubscriptionTier.Plus, result.Tier);
        Assert.Equal(SubscriptionStatus.Active, result.Status);
        Assert.NotNull(result.CurrentPeriodEnd);
    }

    [Fact]
    public async Task GetSubscriptionStatus_FreeUser_ReturnsFreeTier()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User(new Dinder.Domain.ValueObjects.Email("test@example.com"), "hash");

        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _subRepoMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionEntity?)null);

        var handler = new GetSubscriptionStatusQueryHandler(_subRepoMock.Object, _userRepoMock.Object);
        var query = new GetSubscriptionStatusQuery(userId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SubscriptionTier.Free, result.Tier);
        Assert.Null(result.Status);
    }

    #endregion

    #region ProcessStripeWebhook

    [Fact]
    public async Task ProcessWebhook_CheckoutCompleted_ActivatesSubscription()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User(new Dinder.Domain.ValueObjects.Email("test@example.com"), "hash");

        var webhookEvent = new StripeWebhookEvent
        {
            Id = "evt_123",
            Type = "checkout.session.completed",
            SubscriptionId = "sub_abc",
            CustomerId = "cus_xyz",
            UserId = userId,
            Tier = SubscriptionTier.Plus,
            Created = DateTime.UtcNow,
        };

        _stripeServiceMock.Setup(s => s.ConstructWebhookEvent(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(webhookEvent);
        _subRepoMock.Setup(r => r.GetByStripeSubscriptionIdAsync("sub_abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionEntity?)null);
        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = new ProcessStripeWebhookCommandHandler(
            _stripeServiceMock.Object, _subRepoMock.Object, _userRepoMock.Object);
        var command = new ProcessStripeWebhookCommand("{}", "sig");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(SubscriptionTier.Plus, user.Tier);
        Assert.Equal("cus_xyz", user.StripeCustomerId);
        _subRepoMock.Verify(r => r.Add(It.IsAny<SubscriptionEntity>()), Times.Once);
        _userRepoMock.Verify(r => r.Update(user), Times.Once);
        _subRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessWebhook_DuplicateCheckoutCompleted_IsIdempotent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User(new Dinder.Domain.ValueObjects.Email("test@example.com"), "hash");

        var webhookEvent = new StripeWebhookEvent
        {
            Id = "evt_123",
            Type = "checkout.session.completed",
            SubscriptionId = "sub_abc",
            CustomerId = "cus_xyz",
            UserId = userId,
            Tier = SubscriptionTier.Plus,
            Created = DateTime.UtcNow,
        };

        var existing = new SubscriptionEntity(userId, "sub_abc", SubscriptionTier.Plus, DateTime.UtcNow.AddMonths(1));

        _stripeServiceMock.Setup(s => s.ConstructWebhookEvent(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(webhookEvent);
        _subRepoMock.Setup(r => r.GetByStripeSubscriptionIdAsync("sub_abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var handler = new ProcessStripeWebhookCommandHandler(
            _stripeServiceMock.Object, _subRepoMock.Object, _userRepoMock.Object);
        var command = new ProcessStripeWebhookCommand("{}", "sig");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert — no modifications
        _subRepoMock.Verify(r => r.Add(It.IsAny<SubscriptionEntity>()), Times.Never);
        _userRepoMock.Verify(r => r.Update(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task ProcessWebhook_SubscriptionDeleted_CancelsAndRevertsToFree()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User(new Dinder.Domain.ValueObjects.Email("test@example.com"), "hash");
        user.SetTier(SubscriptionTier.Premium); // Currently Premium

        var subscription = new SubscriptionEntity(userId, "sub_abc", SubscriptionTier.Premium, DateTime.UtcNow.AddMonths(1));

        var webhookEvent = new StripeWebhookEvent
        {
            Id = "evt_456",
            Type = "customer.subscription.deleted",
            SubscriptionId = "sub_abc",
            Created = DateTime.UtcNow,
        };

        _stripeServiceMock.Setup(s => s.ConstructWebhookEvent(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(webhookEvent);
        _subRepoMock.Setup(r => r.GetByStripeSubscriptionIdAsync("sub_abc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = new ProcessStripeWebhookCommandHandler(
            _stripeServiceMock.Object, _subRepoMock.Object, _userRepoMock.Object);
        var command = new ProcessStripeWebhookCommand("{}", "sig");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(SubscriptionStatus.Canceled, subscription.Status);
        Assert.Equal(SubscriptionTier.Free, user.Tier);
        _subRepoMock.Verify(r => r.Update(subscription), Times.Once);
        _userRepoMock.Verify(r => r.Update(user), Times.Once);
    }

    #endregion
}
