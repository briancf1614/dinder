using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Domain.ValueObjects;
using Xunit;

namespace Dinder.UnitTests;

public class UserSubscriptionTests
{
    [Fact]
    public void NewUser_DefaultsToFreeTier()
    {
        var user = new User(new Email("test@example.com"), "hashedpassword");

        Assert.Equal(SubscriptionTier.Free, user.Tier);
        Assert.Null(user.StripeCustomerId);
    }

    [Fact]
    public void ExternalUser_DefaultsToFreeTier()
    {
        var user = User.CreateExternal(
            new Email("external@example.com"),
            ExternalProvider.Google,
            "ext-123");

        Assert.Equal(SubscriptionTier.Free, user.Tier);
        Assert.Null(user.StripeCustomerId);
    }

    [Fact]
    public void SetTier_UpdatesTier()
    {
        var user = new User(new Email("test@example.com"), "hashedpassword");

        user.SetTier(SubscriptionTier.Premium);

        Assert.Equal(SubscriptionTier.Premium, user.Tier);
    }

    [Fact]
    public void SetStripeCustomerId_UpdatesCustomerId()
    {
        var user = new User(new Email("test@example.com"), "hashedpassword");

        user.SetStripeCustomerId("cus_12345");

        Assert.Equal("cus_12345", user.StripeCustomerId);
    }

    [Fact]
    public void TierPreserved_ThroughStateChanges()
    {
        var user = new User(new Email("test@example.com"), "hashedpassword");

        user.SetTier(SubscriptionTier.Plus);
        user.SoftDelete();

        // Tier should not be affected by unrelated state changes
        Assert.Equal(SubscriptionTier.Plus, user.Tier);
    }
}
