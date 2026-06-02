using System.Text.Json;
using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Domain.Events;
using Dinder.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Dinder.Application.Gamification.Handlers;

/// <summary>
/// Fire-and-forget handler: tracks daily login streaks and awards streak milestones.
/// Subscribes to UserLoggedInEvent. UTC midnight boundary, action-gated (the event
/// itself signals a login, which counts as a meaningful action for the streak).
/// Caps at 30 days. Awards StreakMaster achievement at 30 days.
/// </summary>
public sealed class StreakHandler : INotificationHandler<UserLoggedInEvent>
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

    public async Task Handle(UserLoggedInEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(notification.UserId, cancellationToken);
            if (user is null)
            {
                _logger.LogWarning("StreakHandler: User {UserId} not found", notification.UserId);
                return;
            }

            var today = notification.Timestamp.Date;

            // Check if already processed today (idempotency)
            if (user.LastStreakDate?.Date == today)
            {
                _logger.LogDebug("StreakHandler: Already processed for {UserId} today", notification.UserId);
                return;
            }

            // Determine if streak should increment or reset
            bool increment;
            if (user.LastStreakDate is null)
            {
                // First login ever
                increment = false; // UpdateStreak will set to 1
            }
            else
            {
                var daysSinceLast = (today - user.LastStreakDate.Value.Date).Days;
                increment = daysSinceLast == 1; // Consecutive day → increment; gap → reset
            }

            user.UpdateStreak(notification.Timestamp, increment);

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "StreakHandler: User {UserId} streak updated to {Streak} (increment={Increment})",
                notification.UserId, user.DailyStreak, increment);

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
            _logger.LogError(ex, "StreakHandler: Failed to process streak for User {UserId}", notification.UserId);
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
