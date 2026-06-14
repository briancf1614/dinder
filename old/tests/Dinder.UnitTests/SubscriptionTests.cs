using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Xunit;

namespace Dinder.UnitTests;

public class SubscriptionTests
{
    [Fact]
    public void Constructor_CreatesSubscription_WithCorrectValues()
    {
        var userId = Guid.NewGuid();
        var stripeId = "sub_123";
        var periodEnd = DateTime.UtcNow.AddMonths(1);

        var subscription = new Subscription(userId, stripeId, SubscriptionTier.Plus, periodEnd);

        Assert.Equal(userId, subscription.UserId);
        Assert.Equal(stripeId, subscription.StripeSubscriptionId);
        Assert.Equal(SubscriptionTier.Plus, subscription.Tier);
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.Equal(periodEnd, subscription.CurrentPeriodEnd);
        Assert.NotEqual(Guid.Empty, subscription.Id);
    }

    [Fact]
    public void Activate_UpdatesTierStatusAndPeriodEnd()
    {
        var subscription = new Subscription(
            Guid.NewGuid(), "sub_123", SubscriptionTier.Free, DateTime.UtcNow.AddMonths(1));

        var newPeriodEnd = DateTime.UtcNow.AddMonths(2);
        subscription.Activate(SubscriptionTier.Premium, newPeriodEnd);

        Assert.Equal(SubscriptionTier.Premium, subscription.Tier);
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.Equal(newPeriodEnd, subscription.CurrentPeriodEnd);
    }

    [Fact]
    public void MarkPastDue_TransitionsStatus()
    {
        var subscription = new Subscription(
            Guid.NewGuid(), "sub_123", SubscriptionTier.Plus, DateTime.UtcNow.AddMonths(1));

        subscription.MarkPastDue();

        Assert.Equal(SubscriptionStatus.PastDue, subscription.Status);
    }

    [Fact]
    public void Cancel_TransitionsStatus()
    {
        var subscription = new Subscription(
            Guid.NewGuid(), "sub_123", SubscriptionTier.Plus, DateTime.UtcNow.AddMonths(1));

        subscription.Cancel();

        Assert.Equal(SubscriptionStatus.Canceled, subscription.Status);
    }

    [Fact]
    public void Expire_TransitionsStatus()
    {
        var subscription = new Subscription(
            Guid.NewGuid(), "sub_123", SubscriptionTier.Plus, DateTime.UtcNow.AddMonths(1));

        subscription.Expire();

        Assert.Equal(SubscriptionStatus.Expired, subscription.Status);
    }

    [Fact]
    public void StatusProgression_ActiveToPastDueToExpired()
    {
        var subscription = new Subscription(
            Guid.NewGuid(), "sub_123", SubscriptionTier.Plus, DateTime.UtcNow.AddMonths(1));

        Assert.Equal(SubscriptionStatus.Active, subscription.Status);

        subscription.MarkPastDue();
        Assert.Equal(SubscriptionStatus.PastDue, subscription.Status);

        subscription.Expire();
        Assert.Equal(SubscriptionStatus.Expired, subscription.Status);
    }

    [Fact]
    public void IsGracePeriodExceeded_WithinGrace_ReturnsFalse()
    {
        var subscription = new Subscription(
            Guid.NewGuid(), "sub_123", SubscriptionTier.Plus, DateTime.UtcNow.AddDays(3));

        subscription.MarkPastDue();

        Assert.False(subscription.IsGracePeriodExceeded());
    }

    [Fact]
    public void IsGracePeriodExceeded_BeyondGrace_ReturnsTrue()
    {
        var periodEndInPast = DateTime.UtcNow.AddDays(-8); // 8 days ago
        var subscription = new Subscription(
            Guid.NewGuid(), "sub_123", SubscriptionTier.Plus, periodEndInPast);

        subscription.MarkPastDue();

        Assert.True(subscription.IsGracePeriodExceeded());
    }

    [Fact]
    public void IsGracePeriodExceeded_NotPastDue_ReturnsFalse()
    {
        var periodEndInPast = DateTime.UtcNow.AddDays(-8);
        var subscription = new Subscription(
            Guid.NewGuid(), "sub_123", SubscriptionTier.Plus, periodEndInPast);

        // Still Active, not PastDue — grace period check should be false
        Assert.False(subscription.IsGracePeriodExceeded());
    }
}
