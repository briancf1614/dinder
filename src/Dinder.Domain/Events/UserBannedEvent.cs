using MediatR;

namespace Dinder.Domain.Events;

public sealed record UserBannedEvent(Guid UserId, Guid AdminId, string Reason) : INotification;
