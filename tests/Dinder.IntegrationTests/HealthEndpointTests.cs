using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Dinder.IntegrationTests;

// WebApplicationFactory levanta la API EN MEMORIA, sin servidor real
// El <InternalsVisibleTo> en Dinder.Api.csproj permite acceder a Program
public class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        // Creamos un cliente HTTP que pega contra la API en memoria
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_Returns200_WithJson()
    {
        // Act: hacemos GET /health
        var response = await _client.GetAsync("/health");

        // Assert: 200 OK
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        // Assert: Content-Type es JSON
        Assert.Contains("application/json", response.Content.Headers.ContentType!.MediaType);

        // Assert: el body contiene status="healthy"
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"healthy\"", body);
    }
}
