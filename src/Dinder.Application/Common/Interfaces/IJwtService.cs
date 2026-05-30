namespace Dinder.Application.Common.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(Guid userId, string email);
    string GenerateRefreshToken();
    (string accessToken, string refreshToken) GenerateTokenPair(Guid userId, string email);
}
