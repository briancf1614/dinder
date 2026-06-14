using Dinder.Domain.Entities;

namespace Dinder.Domain.Interfaces;

public interface ISubscriptionRepository
{
    Task<Subscription?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Subscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default);
    void Add(Subscription subscription);
    void Update(Subscription subscription);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
