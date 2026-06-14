using Dinder.Domain.Enums;
using MediatR;

namespace Dinder.Domain.Events;

public sealed record AchievementUnlockedEvent(Guid UserId, AchievementType Type) : INotification;
