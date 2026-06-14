using Dinder.Application.Profile.Commands;
using Dinder.Application.Profile.Validators;
using Dinder.Domain.Enums;
using Xunit;

namespace Dinder.UnitTests;

public class ProfileValidatorTests
{
    [Fact]
    public void CreateOrUpdateProfile_ValidCommand_PassesValidation()
    {
        var validator = new CreateOrUpdateProfileCommandValidator();
        var command = new CreateOrUpdateProfileCommand(
            Guid.NewGuid(), "Alice", Gender.Female, "Hello world");

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateOrUpdateProfile_EmptyDisplayName_FailsValidation()
    {
        var validator = new CreateOrUpdateProfileCommandValidator();
        var command = new CreateOrUpdateProfileCommand(
            Guid.NewGuid(), "", Gender.Female, null);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "DisplayName");
    }

    [Fact]
    public void CreateOrUpdateProfile_BioTooLong_FailsValidation()
    {
        var validator = new CreateOrUpdateProfileCommandValidator();
        var command = new CreateOrUpdateProfileCommand(
            Guid.NewGuid(), "Alice", Gender.Female, new string('x', 501));

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Bio");
    }

    [Fact]
    public void UpdatePreferences_Valid_PassesValidation()
    {
        var validator = new UpdatePreferencesCommandValidator();
        var command = new UpdatePreferencesCommand(
            Guid.NewGuid(),
            new List<Gender> { Gender.Female },
            25, 40, 50);

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void UpdatePreferences_MinAgeBelow18_FailsValidation()
    {
        var validator = new UpdatePreferencesCommandValidator();
        var command = new UpdatePreferencesCommand(
            Guid.NewGuid(),
            new List<Gender> { Gender.Female },
            17, 40, 50);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "MinAge");
    }

    [Fact]
    public void UpdatePreferences_MaxAgeAbove100_FailsValidation()
    {
        var validator = new UpdatePreferencesCommandValidator();
        var command = new UpdatePreferencesCommand(
            Guid.NewGuid(),
            new List<Gender> { Gender.Female },
            25, 101, 50);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "MaxAge");
    }

    [Fact]
    public void UpdatePreferences_MaxAgeLessThanMinAge_FailsValidation()
    {
        var validator = new UpdatePreferencesCommandValidator();
        var command = new UpdatePreferencesCommand(
            Guid.NewGuid(),
            new List<Gender> { Gender.Female },
            35, 25, 50);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void UpdatePreferences_DistanceAbove500_FailsValidation()
    {
        var validator = new UpdatePreferencesCommandValidator();
        var command = new UpdatePreferencesCommand(
            Guid.NewGuid(),
            new List<Gender> { Gender.Female },
            25, 40, 501);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "MaxDistanceKm");
    }

    [Fact]
    public void UpdateLocation_ValidCoords_PassesValidation()
    {
        var validator = new UpdateProfileLocationCommandValidator();
        var command = new UpdateProfileLocationCommand(Guid.NewGuid(), 40.7128, -74.0060);

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void UpdateLocation_InvalidLatitude_FailsValidation()
    {
        var validator = new UpdateProfileLocationCommandValidator();
        var command = new UpdateProfileLocationCommand(Guid.NewGuid(), 91, 0);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Latitude");
    }
}
