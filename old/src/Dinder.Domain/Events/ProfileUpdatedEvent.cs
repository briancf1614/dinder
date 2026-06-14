using MediatR;

namespace Dinder.Domain.Events;

/// <summary>Fired when a user updates any profile field that affects completeness scoring.</summary>
public sealed record ProfileUpdatedEvent(Guid UserId, DateTime Timestamp) : INotification;
