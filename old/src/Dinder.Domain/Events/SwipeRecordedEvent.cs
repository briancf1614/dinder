using MediatR;

namespace Dinder.Domain.Events;

public sealed record SwipeRecordedEvent(Guid SwipeId, Guid SwiperId, Guid SwipedId, string Direction) : INotification;
