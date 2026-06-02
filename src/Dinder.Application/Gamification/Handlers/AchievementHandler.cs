using System.Text.Json;
using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Domain.Events;
using Dinder.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Dinder.Application.Gamification.Handlers;

/// <summary>
/// Fire-and-forget handler: evaluates achievement criteria on domain events
/// and unlocks badges when thresholds are met. Idempotent — no re-award.
/// Pushes real-time notifications via SignalR when an achievement is unlocked.
/// </summary>
public sealed class AchievementHandler
    : INotificationHandler<SwipeRecordedEvent>,
      INotificationHandler<MatchCreatedEvent>,
      INotificationHandler<MessageSentEvent>,
      INotificationHandler<AchievementUnlockedEvent>
{
    private readonly IUserRepository _userRepository;
    private readonly IDiscoveryRepository _discoveryRepository;
    private readonly IChatRepository _chatRepository;
    private readonly IAchievementRegistry _achievementRegistry;
    private readonly IAchievementPushService _pushService;
    private readonly IMediator _mediator;
    private readonly ILogger<AchievementHandler> _logger;

    public AchievementHandler(
        IUserRepository userRepository,
        IDiscoveryRepository discoveryRepository,
        IChatRepository chatRepository,
        IAchievementRegistry achievementRegistry,
        IAchievementPushService pushService,
        IMediator mediator,
        ILogger<AchievementHandler> logger)
    {
        _userRepository = userRepository;
        _discoveryRepository = discoveryRepository;
        _chatRepository = chatRepository;
        _achievementRegistry = achievementRegistry;
        _pushService = pushService;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(SwipeRecordedEvent notification, CancellationToken cancellationToken)
    {
        await TryUnlockAchievement(notification.SwiperId, AchievementType.CenturySwiper,
            async () =>
            {
                var count = await _discoveryRepository.GetLifetimeSwipeCountAsync(
                    notification.SwiperId, cancellationToken);
                return count >= 100;
            },
            cancellationToken);
    }

    public async Task Handle(MatchCreatedEvent notification, CancellationToken cancellationToken)
    {
        // FirstMatch: unlock for both users
        await TryUnlockAchievement(notification.UserId1, AchievementType.FirstMatch,
            () => Task.FromResult(true), cancellationToken);
        await TryUnlockAchievement(notification.UserId2, AchievementType.FirstMatch,
            () => Task.FromResult(true), cancellationToken);
    }

    public async Task Handle(MessageSentEvent notification, CancellationToken cancellationToken)
    {
        await TryUnlockAchievement(notification.SenderId, AchievementType.SocialButterfly,
            async () =>
            {
                var count = await _chatRepository.GetMessageCountBySenderAsync(
                    notification.SenderId, cancellationToken);
                return count >= 50;
            },
            cancellationToken);
    }

    public async Task Handle(AchievementUnlockedEvent notification, CancellationToken cancellationToken)
    {
        // 1. Persist to User.Achievements JSON
        try
        {
            var user = await _userRepository.GetByIdAsync(notification.UserId, cancellationToken);
            if (user is null) return;

            var achievements = DeserializeAchievements(user.Achievements);
            var achievementTypeStr = notification.Type.ToString();

            if (achievements.Contains(achievementTypeStr))
                return; // Already persisted — idempotent

            // Look up achievement definition for push notification payload
            var definition = _achievementRegistry.GetDefinition(notification.Type);
            if (definition is null)
            {
                _logger.LogWarning("AchievementHandler: No definition found for {Type}", notification.Type);
                return;
            }

            achievements.Add(achievementTypeStr);
            user.SetAchievements(JsonSerializer.Serialize(achievements));
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "AchievementHandler: Persisted {Achievement} for User {UserId}",
                notification.Type, notification.UserId);

            // 2. Push real-time notification via SignalR
            await _pushService.PushAchievementUnlockedAsync(
                notification.UserId,
                definition,
                cancellationToken);

            _logger.LogInformation(
                "AchievementHandler: Push notification sent for {Achievement} to User {UserId}",
                notification.Type, notification.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AchievementHandler: Failed to persist/push {Achievement} for User {UserId}",
                notification.Type, notification.UserId);
        }
    }

    private async Task TryUnlockAchievement(
        Guid userId,
        AchievementType type,
        Func<Task<bool>> criteriaMet,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null) return;

            // Idempotency: check if already unlocked
            var achievements = DeserializeAchievements(user.Achievements);
            if (achievements.Contains(type.ToString()))
            {
                _logger.LogDebug("Achievement {Type} already unlocked for User {UserId}", type, userId);
                return;
            }

            // Check criteria
            if (!await criteriaMet())
                return;

            // Fire the event — persistence + push happens in Handle(AchievementUnlockedEvent)
            await _mediator.Publish(new AchievementUnlockedEvent(userId, type), cancellationToken);

            _logger.LogInformation("Achievement {Type} unlocked for User {UserId}", type, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AchievementHandler: Error evaluating {Type} for User {UserId}", type, userId);
        }
    }

    private static List<string> DeserializeAchievements(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
