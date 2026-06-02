using Dinder.Domain.Enums;

namespace Dinder.Domain.Entities;

public sealed class Subscription
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string StripeSubscriptionId { get; private set; }
    public SubscriptionTier Tier { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public DateTime CurrentPeriodEnd { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

#pragma warning disable CS8618
    private Subscription() { } // EF Core
#pragma warning restore CS8618

    public Subscription(
        Guid userId,
        string stripeSubscriptionId,
        SubscriptionTier tier,
        DateTime currentPeriodEnd)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        StripeSubscriptionId = stripeSubscriptionId;
        Tier = tier;
        Status = SubscriptionStatus.Active;
        CurrentPeriodEnd = currentPeriodEnd;
        CreatedAt = DateTime.UtcNow;
    }

    public void Activate(SubscriptionTier tier, DateTime currentPeriodEnd)
    {
        Tier = tier;
        Status = SubscriptionStatus.Active;
        CurrentPeriodEnd = currentPeriodEnd;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkPastDue()
    {
        Status = SubscriptionStatus.PastDue;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        Status = SubscriptionStatus.Canceled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Expire()
    {
        Status = SubscriptionStatus.Expired;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePeriodEnd(DateTime newPeriodEnd)
    {
        CurrentPeriodEnd = newPeriodEnd;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsGracePeriodExceeded() =>
        Status == SubscriptionStatus.PastDue && DateTime.UtcNow > CurrentPeriodEnd.AddDays(7);
}
