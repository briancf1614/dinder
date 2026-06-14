using Dinder.Domain.ValueObjects;
using Xunit;

namespace Dinder.UnitTests;

public class EmailTests
{
    [Fact]
    public void Constructor_ValidEmail_CreatesInstance()
    {
        var email = new Email("test@example.com");
        Assert.Equal("test@example.com", email.Value);
    }

    [Fact]
    public void Constructor_InvalidEmail_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Email("not-an-email"));
    }

    [Fact]
    public void Constructor_EmptyEmail_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Email(""));
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        var email = new Email("Test@Example.com");
        string value = email;
        Assert.Equal("test@example.com", value); // lowercased
    }
}
