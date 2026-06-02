using Dinder.Domain.Events;
using Dinder.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Dinder.Application.Gamification.Handlers;

/// <summary>
/// Fire-and-forget handler: awards bonus swipes at streak milestones (7, 14, 30 days).
/// Sets User.DailyBonusSwipes which is consumed at swipe time by SwipeCommand.
/// </summary>
public sealed class DailyRewardHandler : INotificationHandler<UserLoggedInEvent>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<DailyRewardHandler> _logger;

    public DailyRewardHandler(IUserRepository userRepository, ILogger<DailyRewardHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task Handle(UserLoggedInEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(notification.UserId, cancellationToken);
            if (user is null) return;

            var today = notification.Timestamp.Date;

            // Only award once per day (idempotency)
            if (user.LastStreakDate?.Date == today)
                return;

            // Determine bonus based on streak milestone
            var bonus = user.DailyStreak switch
            {
                >= 30 => 15,
                >= 14 => 10,
                >= 7 => 5,
                _ => 0
            };

            if (bonus > 0)
            {
                user.SetBonusSwipes(bonus);
                _userRepository.Update(user);
                await _userRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "DailyRewardHandler: Awarded {Bonus} bonus swipes to User {UserId} (streak={Streak})",
                    bonus, notification.UserId, user.DailyStreak);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DailyRewardHandler: Failed for User {UserId}", notification.UserId);
        }
    }
}
