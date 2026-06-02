using System.IdentityModel.Tokens.Jwt;
using Dinder.Infrastructure.Auth;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Dinder.UnitTests;

public class JwtServiceTierTests
{
    private static IConfiguration CreateConfiguration(string secret)
    {
        var configData = new Dictionary<string, string?>
        {
            { "Jwt:Secret", secret },
            { "Jwt:Issuer", "TestIssuer" },
            { "Jwt:Audience", "TestAudience" },
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }

    [Fact]
    public void GenerateAccessToken_WithTier_IncludesTierClaim()
    {
        var config = CreateConfiguration("this-is-a-secret-key-with-32-chars-min!");
        var jwtService = new JwtService(config);

        var token = jwtService.GenerateAccessToken(Guid.NewGuid(), "test@example.com", tier: "Plus");

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Contains(jwt.Claims, c => c.Type == "tier" && c.Value == "Plus");
    }

    [Fact]
    public void GenerateAccessToken_WithoutTier_ExcludesTierClaim()
    {
        var config = CreateConfiguration("this-is-a-secret-key-with-32-chars-min!");
        var jwtService = new JwtService(config);

        var token = jwtService.GenerateAccessToken(Guid.NewGuid(), "test@example.com");

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.DoesNotContain(jwt.Claims, c => c.Type == "tier");
    }

    [Fact]
    public void GenerateTokenPair_WithTier_IncludesTierInAccessToken()
    {
        var config = CreateConfiguration("this-is-a-secret-key-with-32-chars-min!");
        var jwtService = new JwtService(config);

        var (accessToken, refreshToken) = jwtService.GenerateTokenPair(
            Guid.NewGuid(), "test@example.com", tier: "Premium");

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(accessToken);

        Assert.Contains(jwt.Claims, c => c.Type == "tier" && c.Value == "Premium");
        Assert.False(string.IsNullOrEmpty(refreshToken));
    }

    [Fact]
    public void GenerateAccessToken_WithNullTier_ExcludesTierClaim()
    {
        var config = CreateConfiguration("this-is-a-secret-key-with-32-chars-min!");
        var jwtService = new JwtService(config);

        var token = jwtService.GenerateAccessToken(Guid.NewGuid(), "test@example.com", tier: null);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.DoesNotContain(jwt.Claims, c => c.Type == "tier");
    }
}
