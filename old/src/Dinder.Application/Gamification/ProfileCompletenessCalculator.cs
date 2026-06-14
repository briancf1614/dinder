using Dinder.Domain.Entities;

namespace Dinder.Application.Gamification;

/// <summary>
/// Computes a profile completeness score (0–100%) based on four weighted factors:
/// photo uploaded (25%), bio filled (25%), preferences set (25%), prompts answered (25%).
/// </summary>
public static class ProfileCompletenessCalculator
{
    private const int MaxScore = 100;
    private const int FactorCount = 4; // photo, bio, preferences, prompts
    private const int PointsPerFactor = MaxScore / FactorCount; // 25

    /// <summary>
    /// Computes the completeness score for a given profile.
    /// Each factor contributes (100 / 4) = 25 points. The total score is the sum of satisfied factors.
    /// </summary>
    public static int Compute(Domain.Entities.Profile profile)
    {
        var score = 0;

        // 1. Photo uploaded — at least one photo
        if (profile.Photos.Count > 0)
            score += PointsPerFactor;

        // 2. Bio filled — non-null and non-whitespace
        if (!string.IsNullOrWhiteSpace(profile.Bio))
            score += PointsPerFactor;

        // 3. Preferences set — discovery preferences exist
        if (profile.Preference is not null)
            score += PointsPerFactor;

        // 4. At least one prompt answered
        if (profile.Prompts.Count > 0)
            score += PointsPerFactor;

        return score;
    }
}
