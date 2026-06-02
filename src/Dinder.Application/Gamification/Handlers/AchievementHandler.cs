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
    private readonly IMediator _mediator;
    private readonly ILogger<AchievementHandler> _logger;

    public AchievementHandler(
        IUserRepository userRepository,
        IDiscoveryRepository discoveryRepository,
        IChatRepository chatRepository,
        IAchievementRegistry achievementRegistry,
        IMediator mediator,
        ILogger<AchievementHandler> logger)
    {
        _userRepository = userRepository;
        _discoveryRepository = discoveryRepository;
        _chatRepository = chatRepository;
        _achievementRegistry = achievementRegistry;
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

        // SocialButterfly: sender gets message credit (a match creates a conversation)
        // Achievement evaluated on message sent, not match
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
        // Persist the unlocked achievement to User.Achievements JSON
        try
        {
            var user = await _userRepository.GetByIdAsync(notification.UserId, cancellationToken);
            if (user is null) return;

            var achievements = DeserializeAchievements(user.Achievements);
            var achievementTypeStr = notification.Type.ToString();

            if (achievements.Contains(achievementTypeStr))
                return; // Already persisted — idempotent

            achievements.Add(achievementTypeStr);
            user.SetAchievements(JsonSerializer.Serialize(achievements));
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "AchievementHandler: Persisted {Achievement} for User {UserId}",
                notification.Type, notification.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AchievementHandler: Failed to persist {Achievement} for User {UserId}",
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

            // Fire the event — persistence happens in Handle(AchievementUnlockedEvent)
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
