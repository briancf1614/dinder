using MediatR;

namespace Dinder.Domain.Events;

public sealed record MatchCreatedEvent(Guid MatchId, Guid UserId1, Guid UserId2) : INotification;
