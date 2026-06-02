using Dinder.Domain.Interfaces;

namespace Dinder.Application.Gamification;

/// <summary>
/// Pushes achievement unlock notifications to connected clients via SignalR.
/// Implemented in the Infrastructure layer to avoid breaking the Application → Infrastructure
/// dependency rule.
/// </summary>
public interface IAchievementPushService
{
    /// <summary>
    /// Sends a real-time achievement notification to a specific user.
    /// </summary>
    Task PushAchievementUnlockedAsync(Guid userId, AchievementDefinition definition, CancellationToken cancellationToken);
}
