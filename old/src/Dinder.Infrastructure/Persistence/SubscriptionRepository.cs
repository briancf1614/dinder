using Dinder.Domain.Entities;
using Dinder.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Dinder.Infrastructure.Persistence;

public sealed class SubscriptionRepository : ISubscriptionRepository
{
    private readonly SubscriptionDbContext _context;

    public SubscriptionRepository(SubscriptionDbContext context)
    {
        _context = context;
    }

    public async Task<Subscription?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
    }

    public async Task<Subscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default)
    {
        return await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscriptionId, cancellationToken);
    }

    public void Add(Subscription subscription) => _context.Subscriptions.Add(subscription);

    public void Update(Subscription subscription) => _context.Subscriptions.Update(subscription);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
