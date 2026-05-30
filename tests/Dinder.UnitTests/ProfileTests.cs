using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Xunit;

namespace Dinder.UnitTests;

public class ProfileTests
{
    [Fact]
    public void Constructor_ValidParams_CreatesProfile()
    {
        var profile = new Profile(Guid.NewGuid(), "Alice", Gender.Female, new DateOnly(1995, 6, 15));

        Assert.Equal("Alice", profile.DisplayName);
        Assert.Equal(Gender.Female, profile.Gender);
        Assert.Equal(new DateOnly(1995, 6, 15), profile.Birthday);
        Assert.False(profile.IsDiscoverable);
        Assert.NotNull(profile.Photos);
    }

    [Fact]
    public void Update_SetsFieldsAndChecksDiscoverability()
    {
        var profile = new Profile(Guid.NewGuid(), "Bob", Gender.Male, new DateOnly(1990, 1, 1));
        profile.Update("Bobby", Gender.Male, "Hello world!");

        Assert.Equal("Bobby", profile.DisplayName);
        Assert.Equal("Hello world!", profile.Bio);
        // Still not discoverable — no photos or preferences
        Assert.False(profile.IsDiscoverable);
    }

    [Fact]
    public void SetLocation_StoresPostGISPoint()
    {
        var profile = new Profile(Guid.NewGuid(), "Carol", Gender.Female, new DateOnly(1998, 3, 20));
        profile.SetLocation(40.7128, -74.0060);

        Assert.NotNull(profile.Location);
        Assert.Equal(4326, profile.Location!.SRID);
        Assert.Equal(40.7128, profile.Location.Y); // latitude
        Assert.Equal(-74.0060, profile.Location.X); // longitude
    }

    [Fact]
    public void IsDiscoverable_True_WhenBioAndPrefsAndPhotoPresent()
    {
        var profile = new Profile(Guid.NewGuid(), "Dave", Gender.Male, new DateOnly(1992, 7, 22));
        profile.Update("Dave", Gender.Male, "This is my bio");
        profile.SetPreference(new ProfilePreference(
            profile.Id,
            new List<Gender> { Gender.Female },
            25, 40, 50));
        profile.AddPhoto(new ProfilePhoto(profile.Id, Guid.NewGuid(), 0));

        Assert.True(profile.IsDiscoverable);
    }

    [Fact]
    public void IsDiscoverable_False_WhenBioMissing()
    {
        var profile = new Profile(Guid.NewGuid(), "Eve", Gender.Female, new DateOnly(1994, 11, 3));
        profile.SetPreference(new ProfilePreference(
            profile.Id,
            new List<Gender> { Gender.Male },
            25, 35, 30));
        profile.AddPhoto(new ProfilePhoto(profile.Id, Guid.NewGuid(), 0));

        Assert.False(profile.IsDiscoverable);
    }

    [Fact]
    public void GetAge_ReturnsCorrectAge()
    {
        var birthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-30).AddDays(1));
        var profile = new Profile(Guid.NewGuid(), "Frank", Gender.Male, birthDate);

        var age = profile.GetAge();
        Assert.Equal(29, age); // birthday hasn't happened yet this year (by 1 day)
    }

    [Fact]
    public void ReorderPhotos_UpdatesSortOrder()
    {
        var profile = new Profile(Guid.NewGuid(), "Grace", Gender.Female, new DateOnly(1996, 5, 10));
        var photo1 = new ProfilePhoto(profile.Id, Guid.NewGuid(), 0);
        var photo2 = new ProfilePhoto(profile.Id, Guid.NewGuid(), 1);
        var photo3 = new ProfilePhoto(profile.Id, Guid.NewGuid(), 2);
        profile.AddPhoto(photo1);
        profile.AddPhoto(photo2);
        profile.AddPhoto(photo3);

        profile.ReorderPhotos(new List<Guid> { photo3.Id, photo1.Id, photo2.Id });

        Assert.Equal(0, photo3.SortOrder);
        Assert.Equal(1, photo1.SortOrder);
        Assert.Equal(2, photo2.SortOrder);
    }

    [Fact]
    public void Preference_Update_ChangesAllFields()
    {
        var pref = new ProfilePreference(Guid.NewGuid(),
            new List<Gender> { Gender.Female }, 25, 35, 30);

        pref.Update(new List<Gender> { Gender.Female, Gender.NonBinary }, 22, 40, 50);

        Assert.Equal(2, pref.InterestedInGenders.Count);
        Assert.Equal(22, pref.MinAge);
        Assert.Equal(40, pref.MaxAge);
        Assert.Equal(50, pref.MaxDistanceKm);
    }
}
