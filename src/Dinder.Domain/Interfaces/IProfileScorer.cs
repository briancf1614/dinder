using Dinder.Domain.Entities;
using Dinder.Domain.ValueObjects;

namespace Dinder.Domain.Interfaces;

/// <summary>Ranks candidate profiles by similarity to the user's profile.</summary>
public interface IProfileScorer
{
    /// <summary>
    /// Score each candidate against the user's profile and return them in ranked order (highest first).
    /// </summary>
    Task<IReadOnlyList<ScoredProfile>> ScoreAsync(
        Profile user,
        List<Profile> candidates,
        CancellationToken cancellationToken = default);
}
