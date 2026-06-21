using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Dinder.IntegrationTests;

public class RootEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public RootEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRoot_Returns200_WithBody()
    {
        // Act: GET /
        var response = await _client.GetAsync("/");

        // Assert: 200 OK
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        // Assert: body no vacío
        var body = await response.Content.ReadAsStringAsync();
        Assert.NotNull(body);
        Assert.NotEmpty(body);
    }
}
