using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Dinder.IntegrationTests;

[Collection("Database")]
public class ProfileEndpointTests
{
    private readonly HttpClient _client;

    public ProfileEndpointTests(DatabaseFixture fixture)
    {
        _client = new CustomWebApplicationFactory(fixture.ConnectionString)
            .CreateClient();
    }

    /// <summary>
    /// PUT /me/profile → GET /me round-trip: profile persists and GET returns 7 fields.
    /// </summary>
    [Fact]
    public async Task PutProfile_ThenGetMe_RoundTrip_Returns7Fields()
    {
        // ── Arrange: register and get JWT ──
        var email = $"profile-rt-{Guid.NewGuid():N}@test.com";
        var password = "Profile123!";

        var registerResp = await _client.PostAsJsonAsync("/auth/register", new { email, password });
        registerResp.EnsureSuccessStatusCode();
        var registerJson = await registerResp.Content.ReadAsStringAsync();
        using var regDoc = JsonDocument.Parse(registerJson);
        var token = regDoc.RootElement.GetProperty("token").GetString();

        var authHeader = new AuthenticationHeaderValue("Bearer", token);

        // ── Act: PUT /me/profile with all 4 fields ──
        var profilePayload = new
        {
            displayName = "RoundTripUser",
            bio = "Testing round-trip persistence",
            birthDate = "1990-03-20",
            gender = "Female"
        };

        using var putRequest = new HttpRequestMessage(HttpMethod.Put, "/me/profile")
        {
            Content = JsonContent.Create(profilePayload)
        };
        putRequest.Headers.Authorization = authHeader;
        var putResponse = await _client.SendAsync(putRequest);

        // ── Assert: 200 OK ──
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        // ── Assert: PUT response has 7 fields ──
        var putBody = await putResponse.Content.ReadAsStringAsync();
        using var putDoc = JsonDocument.Parse(putBody);
        var putRoot = putDoc.RootElement;

        Assert.True(putRoot.TryGetProperty("id", out _));
        Assert.True(putRoot.TryGetProperty("email", out _));
        Assert.True(putRoot.TryGetProperty("createdAt", out _));
        Assert.True(putRoot.TryGetProperty("displayName", out var putDn));
        Assert.Equal("RoundTripUser", putDn.GetString());
        Assert.True(putRoot.TryGetProperty("bio", out var putBio));
        Assert.Equal("Testing round-trip persistence", putBio.GetString());
        Assert.True(putRoot.TryGetProperty("birthDate", out var putBd));
        Assert.Equal("1990-03-20", putBd.GetString());
        Assert.True(putRoot.TryGetProperty("gender", out var putGender));
        Assert.Equal("Female", putGender.GetString());

        // ── Act: GET /me ──
        using var getRequest = new HttpRequestMessage(HttpMethod.Get, "/me");
        getRequest.Headers.Authorization = authHeader;
        var getResponse = await _client.SendAsync(getRequest);

        // ── Assert: GET /me returns 7 fields with persisted values ──
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var getBody = await getResponse.Content.ReadAsStringAsync();
        using var getDoc = JsonDocument.Parse(getBody);
        var getRoot = getDoc.RootElement;

        Assert.True(getRoot.TryGetProperty("displayName", out var getDn));
        Assert.Equal("RoundTripUser", getDn.GetString());
        Assert.True(getRoot.TryGetProperty("bio", out var getBio));
        Assert.Equal("Testing round-trip persistence", getBio.GetString());
        Assert.True(getRoot.TryGetProperty("birthDate", out var getBd));
        Assert.Equal("1990-03-20", getBd.GetString());
        Assert.True(getRoot.TryGetProperty("gender", out var getGender));
        Assert.Equal("Female", getGender.GetString());
    }

    /// <summary>
    /// PUT /me/profile without JWT → 401 Unauthorized.
    /// </summary>
    [Fact]
    public async Task PutProfile_WithoutToken_Returns401()
    {
        // ── Act ──
        var payload = new
        {
            displayName = "NoAuth",
            bio = (string?)null,
            birthDate = (string?)null,
            gender = (string?)null
        };
        var response = await _client.PutAsJsonAsync("/me/profile", payload);

        // ── Assert ──
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// PUT /me/profile with DisplayName > 100 chars → 400 Bad Request.
    /// </summary>
    [Fact]
    public async Task PutProfile_DisplayNameTooLong_Returns400()
    {
        // ── Arrange: register and get JWT ──
        var email = $"profile-long-{Guid.NewGuid():N}@test.com";
        var password = "Profile123!";

        var registerResp = await _client.PostAsJsonAsync("/auth/register", new { email, password });
        registerResp.EnsureSuccessStatusCode();
        var registerJson = await registerResp.Content.ReadAsStringAsync();
        using var regDoc = JsonDocument.Parse(registerJson);
        var token = regDoc.RootElement.GetProperty("token").GetString();

        // ── Act: PUT with DisplayName > 100 chars ──
        var tooLongName = new string('A', 101);
        var payload = new
        {
            displayName = tooLongName,
            bio = (string?)null,
            birthDate = (string?)null,
            gender = (string?)null
        };

        using var request = new HttpRequestMessage(HttpMethod.Put, "/me/profile")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        // ── Assert ──
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
