using MediatR;

namespace Dinder.Domain.Events;

public sealed record SubscriptionActivatedEvent(Guid SubscriptionId, Guid UserId, string Tier) : INotification;
