using Dinder.Domain.Entities;
using Dinder.Domain.ValueObjects;
using Xunit;

namespace Dinder.UnitTests;

public class UserAgeGateTests
{
    [Fact]
    public void IsAgeGated_Under18_ReturnsTrue()
    {
        var youngDOB = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-16));
        var user = new User(new Email("test@example.com"), "hash", youngDOB);

        Assert.True(user.IsAgeGated());
    }

    [Fact]
    public void IsAgeGated_Exactly18_ReturnsFalse()
    {
        var exact18 = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-18));
        var user = new User(new Email("test@example.com"), "hash", exact18);

        Assert.False(user.IsAgeGated());
    }

    [Fact]
    public void IsAgeGated_Over18_ReturnsFalse()
    {
        var older = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25));
        var user = new User(new Email("test@example.com"), "hash", older);

        Assert.False(user.IsAgeGated());
    }

    [Fact]
    public void GetAge_WithNullBirthday_ReturnsZero()
    {
        var user = new User(new Email("test@example.com"), "hash");

        Assert.Equal(0, user.GetAge());
    }
}
