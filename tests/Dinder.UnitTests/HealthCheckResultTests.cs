using Dinder.Application.Common.Models;
using Xunit;
namespace Dinder.UnitTests;

public class HealthCheckResultTests
{
    [Fact]
    public void Constructor_WhenCalled_HasDefaultValues()
    {
        var result = new HealthCheckResult();
        Assert.Equal(string.Empty, result.Status);
        Assert.Equal(DateTime.MinValue, result.Timestamp);
    }
}