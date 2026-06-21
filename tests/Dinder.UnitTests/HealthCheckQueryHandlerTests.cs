using Dinder.Application.Common.Queries.HealthCheck;
using Xunit;
namespace Dinder.UnitTests;

public class HealthCheckQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenCalled_ReturnsHealthyStatus()
    {
        // Arrange: creás el handler (no necesita nada)
        var handler = new HealthCheckQueryHandler();
        // Act: lo ejecutás
        var result = await handler.Handle(new HealthCheckQuery(), CancellationToken.None);
        // Assert: verificás que devuelve "healthy"
        Assert.Equal("healthy", result.Status);
    }
    [Fact]
    public async Task Handle_WhenCalled_ReturnsRecentTimestamp()
    {
        // Arrange
        var before = DateTime.UtcNow;
        var handler = new HealthCheckQueryHandler();
        // Act
        var result = await handler.Handle(new HealthCheckQuery(), CancellationToken.None);
        // Assert: el timestamp tiene que ser después de "before" y dentro de 5 segundos
        Assert.True(result.Timestamp >= before);
        Assert.True(result.Timestamp <= DateTime.UtcNow.AddSeconds(5));
    }
}