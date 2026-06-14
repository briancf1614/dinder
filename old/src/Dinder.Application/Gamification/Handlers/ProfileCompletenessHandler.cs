using System.Text.Json;
using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Domain.Events;
using Dinder.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Dinder.Application.Gamification.Handlers;

/// <summary>
/// Fire-and-forget handler: recomputes profile completeness score when the profile
/// is updated, and evaluates the ProfileComplete achievement when the score reaches 100%.
/// Subscribes to ProfileUpdatedEvent (bio, preferences, prompts, location changes)
/// and PhotoUploadedEvent (photo additions).
/// </summary>
public sealed class ProfileCompletenessHandler
    : INotificationHandler<ProfileUpdatedEvent>,
      INotificationHandler<PhotoUploadedEvent>
{
    private readonly IUserRepository _userRepository;
    private readonly IProfileRepository _profileRepository;
    private readonly IAchievementRegistry _achievementRegistry;
    private readonly IMediator _mediator;
    private readonly ILogger<ProfileCompletenessHandler> _logger;

    public ProfileCompletenessHandler(
        IUserRepository userRepository,
        IProfileRepository profileRepository,
        IAchievementRegistry achievementRegistry,
        IMediator mediator,
        ILogger<ProfileCompletenessHandler> logger)
    {
        _userRepository = userRepository;
        _profileRepository = profileRepository;
        _achievementRegistry = achievementRegistry;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(ProfileUpdatedEvent notification, CancellationToken cancellationToken)
    {
        await EvaluateCompleteness(notification.UserId, cancellationToken);
    }

    public async Task Handle(PhotoUploadedEvent notification, CancellationToken cancellationToken)
    {
        await EvaluateCompleteness(notification.OwnerId, cancellationToken);
    }

    private async Task EvaluateCompleteness(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            var profile = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);
            if (profile is null)
            {
                _logger.LogWarning("ProfileCompletenessHandler: Profile not found for User {UserId}", userId);
                return;
            }

            var score = ProfileCompletenessCalculator.Compute(profile);

            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user is null) return;

            user.SetCompletenessScore(score);
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "ProfileCompletenessHandler: User {UserId} completeness score updated to {Score}%",
                userId, score);

            // Award ProfileComplete achievement when score reaches 100%
            if (score == 100 && !HasAchievement(user, AchievementType.ProfileComplete))
            {
                await _mediator.Publish(
                    new AchievementUnlockedEvent(userId, AchievementType.ProfileComplete),
                    cancellationToken);
                _logger.LogInformation(
                    "ProfileCompletenessHandler: ProfileComplete awarded to User {UserId}", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ProfileCompletenessHandler: Failed for User {UserId}", userId);
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
