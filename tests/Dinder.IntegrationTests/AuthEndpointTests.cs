using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Dinder.IntegrationTests;

[Collection("Database")]
public class AuthEndpointTests
{
    private readonly HttpClient _client;

    public AuthEndpointTests(DatabaseFixture fixture)
    {
        _client = new CustomWebApplicationFactory(fixture.ConnectionString)
            .CreateClient();
    }

    [Fact]
    public async Task Register_Returns200_WithTokenAndRefreshToken()
    {
        // ── Arrange: payload con email único ──
        var payload = new { email = $"reg-{Guid.NewGuid():N}@test.com", password = "Test1234!" };

        // ── Act ──
        var response = await _client.PostAsJsonAsync("/auth/register", payload);

        // ── Assert ──
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("token", out var token));
        Assert.False(string.IsNullOrEmpty(token.GetString()));

        Assert.True(root.TryGetProperty("refreshToken", out var refreshToken));
        Assert.False(string.IsNullOrEmpty(refreshToken.GetString()));
    }

    [Fact]
    public async Task Login_Returns200_WithTokenAndRefreshToken()
    {
        // ── Arrange: primero registramos un usuario ──
        var email = $"login-{Guid.NewGuid():N}@test.com";
        var password = "LoginPass1!";
        var registerPayload = new { email, password };
        await _client.PostAsJsonAsync("/auth/register", registerPayload);

        // ── Act: ahora hacemos login ──
        var loginPayload = new { email, password };
        var response = await _client.PostAsJsonAsync("/auth/login", loginPayload);

        // ── Assert ──
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("token", out var token));
        Assert.False(string.IsNullOrEmpty(token.GetString()));

        Assert.True(root.TryGetProperty("refreshToken", out var refreshToken));
        Assert.False(string.IsNullOrEmpty(refreshToken.GetString()));
    }
}
