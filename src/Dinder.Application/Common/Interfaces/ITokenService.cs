using Dinder.Domain.Entities;

namespace Dinder.Application.Common.Interfaces
{
    public interface ITokenService
    {
        string GenerateToken(User user);
        string GenerateRefreshToken();
    }
}
