using Dinder.Application.Gamification;
using Dinder.Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Dinder.Infrastructure.SignalR;

/// <summary>
/// Pushes achievement unlock notifications via the existing NotificationHub.
/// Implements IAchievementPushService to keep the Application layer decoupled from Infrastructure.
/// </summary>
public sealed class AchievementPushService : IAchievementPushService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<AchievementPushService> _logger;

    public AchievementPushService(
        IHubContext<NotificationHub> hubContext,
        ILogger<AchievementPushService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task PushAchievementUnlockedAsync(
        Guid userId,
        AchievementDefinition definition,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            type = "achievement",
            achievement = definition.Type.ToString(),
            title = definition.Name,
            description = definition.Description,
            iconKey = definition.IconKey,
            unlockedAt = DateTime.UtcNow
        };

        await NotificationHub.SendNotificationAsync(_hubContext, userId, payload, _logger);
    }
}
