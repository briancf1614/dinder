using Dinder.Application.Gamification;
using Dinder.Domain.Entities;
using Xunit;

namespace Dinder.UnitTests;

public class ProfileCompletenessCalculatorTests
{
    [Fact]
    public void PartialProfile_PhotoAndBioOnly_Returns50Percent()
    {
        // Arrange — user has photo and bio but no preferences or prompts
        var profile = CreateProfile(hasPhoto: true, hasBio: true, hasPreferences: false, hasPrompts: false);

        // Act
        var score = ProfileCompletenessCalculator.Compute(profile);

        // Assert
        Assert.Equal(50, score);
    }

    [Fact]
    public void FullyCompleteProfile_AllFields_Returns100Percent()
    {
        // Arrange — all four factors present
        var profile = CreateProfile(hasPhoto: true, hasBio: true, hasPreferences: true, hasPrompts: true);

        // Act
        var score = ProfileCompletenessCalculator.Compute(profile);

        // Assert
        Assert.Equal(100, score);
    }

    [Fact]
    public void EmptyProfile_NothingFilled_Returns0()
    {
        // Arrange
        var profile = CreateProfile(hasPhoto: false, hasBio: false, hasPreferences: false, hasPrompts: false);

        // Act
        var score = ProfileCompletenessCalculator.Compute(profile);

        // Assert
        Assert.Equal(0, score);
    }

    [Fact]
    public void BioAndPreferencesOnly_NoPhotoNoPrompts_Returns50Percent()
    {
        // Arrange
        var profile = CreateProfile(hasPhoto: false, hasBio: true, hasPreferences: true, hasPrompts: false);

        // Act
        var score = ProfileCompletenessCalculator.Compute(profile);

        // Assert
        Assert.Equal(50, score);
    }

    [Fact]
    public void PhotoAndPromptsOnly_NoBioNoPreferences_Returns50Percent()
    {
        // Arrange
        var profile = CreateProfile(hasPhoto: true, hasBio: false, hasPreferences: false, hasPrompts: true);

        // Act
        var score = ProfileCompletenessCalculator.Compute(profile);

        // Assert
        Assert.Equal(50, score);
    }

    [Fact]
    public void ThreeOutOfFour_Returns75Percent()
    {
        // Arrange
        var profile = CreateProfile(hasPhoto: true, hasBio: true, hasPreferences: true, hasPrompts: false);

        // Act
        var score = ProfileCompletenessCalculator.Compute(profile);

        // Assert
        Assert.Equal(75, score);
    }

    [Fact]
    public void OneOutOfFour_Returns25Percent()
    {
        // Arrange
        var profile = CreateProfile(hasPhoto: false, hasBio: true, hasPreferences: false, hasPrompts: false);

        // Act
        var score = ProfileCompletenessCalculator.Compute(profile);

        // Assert
        Assert.Equal(25, score);
    }

    private static Profile CreateProfile(bool hasPhoto, bool hasBio, bool hasPreferences, bool hasPrompts)
    {
        var profile = new Profile(
            Guid.NewGuid(),
            "TestUser",
            Domain.Enums.Gender.Male,
            new DateOnly(1995, 1, 1));

        // Bio
        if (hasBio)
        {
            profile.Update("TestUser", Domain.Enums.Gender.Male, "This is my bio");
        }

        // Photo — add via entity method
        if (hasPhoto)
        {
            profile.AddPhoto(new ProfilePhoto(profile.Id, Guid.NewGuid(), 0));
        }

        // Preferences
        if (hasPreferences)
        {
            var pref = new ProfilePreference(
                profile.Id,
                [Domain.Enums.Gender.Female],
                25, 45, 50);
            profile.SetPreference(pref);
        }

        // Prompts
        if (hasPrompts)
        {
            profile.SetPrompts([
                new ProfilePrompt(Guid.NewGuid(), "I love hiking on weekends", 0)
            ]);
        }

        return profile;
    }
}
