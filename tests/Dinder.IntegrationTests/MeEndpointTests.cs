using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Dinder.IntegrationTests;

[Collection("Database")]
public class MeEndpointTests
{
    private readonly HttpClient _client;

    public MeEndpointTests(DatabaseFixture fixture)
    {
        _client = new CustomWebApplicationFactory(fixture.ConnectionString)
            .CreateClient();
    }

    [Fact]
    public async Task Me_WithValidToken_Returns200_WithUserInfo()
    {
        // ── Arrange: registramos y obtenemos el token ──
        var email = $"me-valid-{Guid.NewGuid():N}@test.com";
        var password = "MeTest123!";
        var registerPayload = new { email, password };

        var registerResponse = await _client.PostAsJsonAsync("/auth/register", registerPayload);
        registerResponse.EnsureSuccessStatusCode();

        var registerJson = await registerResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(registerJson);
        var token = doc.RootElement.GetProperty("token").GetString();

        // ── Act: GET /me con el token ──
        using var request = new HttpRequestMessage(HttpMethod.Get, "/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        // ── Assert ──
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var meDoc = JsonDocument.Parse(body);
        var root = meDoc.RootElement;

        Assert.True(root.TryGetProperty("id", out var id));
        Assert.NotEqual(Guid.Empty.ToString(), id.GetString());

        Assert.True(root.TryGetProperty("email", out var responseEmail));
        Assert.Equal(email, responseEmail.GetString());

        Assert.True(root.TryGetProperty("createdAt", out _));
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        // ── Act: GET /me SIN token ──
        var response = await _client.GetAsync("/me");

        // ── Assert: 401 Unauthorized ──
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
