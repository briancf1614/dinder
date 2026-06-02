using System.Text.Json;
using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Domain.Events;
using Dinder.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Dinder.Application.Gamification.Handlers;

/// <summary>
/// Fire-and-forget handler: tracks daily action streaks and awards streak milestones.
/// Subscribes to SwipeRecordedEvent and MessageSentEvent — the first meaningful action
/// (swipe or message) of each UTC day increments the streak.
/// Caps at 30 days. Awards StreakMaster achievement at 30 days.
/// </summary>
public sealed class StreakHandler
    : INotificationHandler<SwipeRecordedEvent>,
      INotificationHandler<MessageSentEvent>
{
    private readonly IUserRepository _userRepository;
    private readonly IAchievementRegistry _achievementRegistry;
    private readonly IMediator _mediator;
    private readonly ILogger<StreakHandler> _logger;

    public StreakHandler(
        IUserRepository userRepository,
        IAchievementRegistry achievementRegistry,
        IMediator mediator,
        ILogger<StreakHandler> logger)
    {
        _userRepository = userRepository;
        _achievementRegistry = achievementRegistry;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(SwipeRecordedEvent notification, CancellationToken cancellationToken)
    {
        await ProcessAction(notification.SwiperId, notification, cancellationToken);
    }

    public async Task Handle(MessageSentEvent notification, CancellationToken cancellationToken)
    {
        await ProcessAction(notification.SenderId, notification, cancellationToken);
    }

    private async Task ProcessAction(Guid userId, INotification notification, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null)
            {
                _logger.LogWarning("StreakHandler: User {UserId} not found", userId);
                return;
            }

            var now = DateTime.UtcNow;
            var today = now.Date;

            // Check if already processed today (idempotency — first action of the day counts)
            if (user.LastStreakDate?.Date == today)
            {
                _logger.LogDebug("StreakHandler: Already processed for {UserId} today", userId);
                return;
            }

            // Determine if streak should increment or reset
            bool increment;
            if (user.LastStreakDate is null)
            {
                // First action ever
                increment = false; // UpdateStreak will set to 1
            }
            else
            {
                var daysSinceLast = (today - user.LastStreakDate.Value.Date).Days;
                increment = daysSinceLast == 1; // Consecutive day → increment; gap → reset
            }

            user.UpdateStreak(now, increment);

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "StreakHandler: User {UserId} streak updated to {Streak} (increment={Increment})",
                userId, user.DailyStreak, increment);

            // Award StreakMaster achievement at 30-day milestone
            if (user.DailyStreak >= 30 && !HasAchievement(user, AchievementType.StreakMaster))
            {
                await _mediator.Publish(
                    new AchievementUnlockedEvent(user.Id, AchievementType.StreakMaster),
                    cancellationToken);
                _logger.LogInformation("StreakHandler: StreakMaster awarded to User {UserId}", user.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "StreakHandler: Failed to process streak for User {UserId}", userId);
        }
    }

    private static bool HasAchievement(User user, AchievementType type)
    {
        if (string.IsNullOrWhiteSpace(user.Achievements))
            return false;

        try
        {
            var achievements = JsonSerializer.Deserialize<List<string>>(user.Achievements);
            return achievements?.Contains(type.ToString()) == true;
        }
        catch
        {
            return false;
        }
    }
}
