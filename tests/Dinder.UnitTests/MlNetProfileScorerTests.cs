using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Infrastructure.Matching;
using NetTopologySuite.Geometries;
using Xunit;

namespace Dinder.UnitTests;

public class MlNetProfileScorerTests
{
    [Fact]
    public async Task IdenticalProfiles_ScoreHigh()
    {
        // Arrange
        var user = CreateProfile("Alice", 25, Gender.Female, 40.7128, -74.0060,
            ["I love hiking and travel"]);
        var candidate = CreateProfile("AliceClone", 25, Gender.Female, 40.7128, -74.0060,
            ["I love hiking and travel"]);

        var scorer = new MlNetProfileScorer();

        // Act
        var results = await scorer.ScoreAsync(user, [candidate], CancellationToken.None);

        // Assert
        Assert.Single(results);
        Assert.Equal(candidate.Id, results[0].ProfileId);
        Assert.True(results[0].Score > 0.8, $"Expected high similarity, got {results[0].Score}");
    }

    [Fact]
    public async Task VeryDifferentProfiles_ScoreLow()
    {
        // Arrange
        var user = CreateProfile("Alice", 25, Gender.Female, 40.7128, -74.0060,
            ["I love hiking"]);
        var candidate = CreateProfile("Bob", 45, Gender.Male, 35.6762, -139.6503, // Tokyo
            ["I collect stamps"]);

        var scorer = new MlNetProfileScorer();

        // Act
        var results = await scorer.ScoreAsync(user, [candidate], CancellationToken.None);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].Score < 0.5, $"Expected low similarity, got {results[0].Score}");
    }

    [Fact]
    public async Task ColdStart_NoPrompts_StillScoresOnDemographics()
    {
        // Arrange
        var user = CreateProfile("Alice", 25, Gender.Female, 40.7128, -74.0060, []);
        var candidate = CreateProfile("Carol", 26, Gender.Female, 40.7138, -74.0070, []);

        var scorer = new MlNetProfileScorer();

        // Act
        var results = await scorer.ScoreAsync(user, [candidate], CancellationToken.None);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].Score > 0.5,
            $"Expected demographic-only score above 0.5, got {results[0].Score}");
    }

    [Fact]
    public async Task MultipleCandidates_ReturnsRankedByScore()
    {
        // Arrange
        var user = CreateProfile("Alice", 25, Gender.Female, 40.7128, -74.0060,
            ["I love hiking and coffee"]);

        var close = CreateProfile("CloseMatch", 26, Gender.Female, 40.7138, -74.0070,
            ["I love hiking too!"]);

        var far = CreateProfile("FarMatch", 45, Gender.Male, 35.6762, -139.6503,
            ["I collect stamps"]);

        var scorer = new MlNetProfileScorer();

        // Act
        var results = await scorer.ScoreAsync(user, [far, close], CancellationToken.None);

        // Assert — ranked: closest first
        Assert.Equal(2, results.Count);
        Assert.Equal(close.Id, results[0].ProfileId); // Higher similarity should come first
        Assert.True(results[0].Score > results[1].Score,
            $"Close match should score higher than far match ({results[0].Score} vs {results[1].Score})");
    }

    [Fact]
    public async Task EmptyCandidates_ReturnsEmptyList()
    {
        // Arrange
        var user = CreateProfile("Alice", 25, Gender.Female, 40.7128, -74.0060, []);
        var scorer = new MlNetProfileScorer();

        // Act
        var results = await scorer.ScoreAsync(user, [], CancellationToken.None);

        // Assert
        Assert.Empty(results);
    }

    private static Profile CreateProfile(
        string displayName, int age, Gender gender,
        double lat, double lon, string[] promptAnswers)
    {
        var birthday = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-age));
        var profile = new Profile(Guid.NewGuid(), displayName, gender, birthday);

        // Set location via SetLocation which is public
        profile.SetLocation(lat, lon);
        profile.UpdateDiscoverability();

        // Set prompts via reflection (SetPrompts clears and re-adds)
        if (promptAnswers.Length > 0)
        {
            var prompts = promptAnswers.Select(a =>
                new ProfilePrompt(Guid.NewGuid(), a, 0)).ToList();
            profile.SetPrompts(prompts);
        }

        return profile;
    }
}
