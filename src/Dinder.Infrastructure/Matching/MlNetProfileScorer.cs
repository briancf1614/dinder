using Dinder.Domain.Entities;
using Dinder.Domain.Interfaces;
using Dinder.Domain.ValueObjects;

namespace Dinder.Infrastructure.Matching;

/// <summary>
/// Simple cosine-similarity-based profile scorer (no ML.NET dependency).
/// Computes similarity on: age proximity, interest overlap (prompts), location distance.
/// Falls back to demographic-only scoring for cold-start users.
/// </summary>
public sealed class MlNetProfileScorer : IProfileScorer
{
    public Task<IReadOnlyList<ScoredProfile>> ScoreAsync(
        Profile user,
        List<Profile> candidates,
        CancellationToken cancellationToken = default)
    {
        var scored = candidates
            .Select(c => new ScoredProfile(c.Id, ComputeSimilarity(user, c)))
            .OrderByDescending(s => s.Score)
            .ToList();

        return Task.FromResult<IReadOnlyList<ScoredProfile>>(scored.AsReadOnly());
    }

    private static double ComputeSimilarity(Profile user, Profile candidate)
    {
        double score = 0;
        int features = 0;

        // 1. Age proximity (closer = higher score, max diff 20 years)
        var ageDiff = Math.Abs(user.GetAge() - candidate.GetAge());
        var ageScore = Math.Max(0, 1.0 - (ageDiff / 20.0));
        score += ageScore;
        features++;

        // 2. Interest overlap via prompt answers (simple term overlap)
        var userPrompts = user.Prompts.Select(p => p.Answer.ToLowerInvariant()).ToList();
        var candidatePrompts = candidate.Prompts.Select(p => p.Answer.ToLowerInvariant()).ToList();

        if (userPrompts.Count > 0 && candidatePrompts.Count > 0)
        {
            var overlap = CountTermOverlap(userPrompts, candidatePrompts);
            // Normalize: max possible overlap is min(prompt count)
            var maxOverlap = Math.Min(userPrompts.Count, candidatePrompts.Count);
            var interestScore = maxOverlap > 0 ? overlap / (double)maxOverlap : 0;
            score += interestScore;
            features++;
        }

        // 3. Location proximity (closer = higher score, max 100km)
        if (user.Location is not null && candidate.Location is not null)
        {
            var distanceKm = CalculateDistanceKm(
                user.Location.Y, user.Location.X,
                candidate.Location.Y, candidate.Location.X);
            var locationScore = Math.Max(0, 1.0 - (distanceKm / 100.0));
            score += locationScore;
            features++;
        }

        // 4. Gender compatibility bonus (0.1 if preferred gender matches)
        if (user.Preference?.InterestedInGenders?.Contains(candidate.Gender) == true)
        {
            score += 0.1;
            features++;
        }

        // If no features computed, return a neutral score
        if (features == 0) return 0.5;

        return score / features;
    }

    private static int CountTermOverlap(List<string> a, List<string> b)
    {
        var count = 0;
        foreach (var textA in a)
        {
            var wordsA = textA.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var textB in b)
            {
                var wordsB = textB.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                count += wordsA.Intersect(wordsB, StringComparer.OrdinalIgnoreCase).Count();
            }
        }
        return count;
    }

    private static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        // Haversine formula
        const double R = 6371.0; // Earth radius in km
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRad(double deg) => deg * Math.PI / 180.0;
}
