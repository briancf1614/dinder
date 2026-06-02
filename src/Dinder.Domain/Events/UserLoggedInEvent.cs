using MediatR;

namespace Dinder.Domain.Events;

public sealed record UserLoggedInEvent(Guid UserId, DateTime Timestamp) : INotification;
