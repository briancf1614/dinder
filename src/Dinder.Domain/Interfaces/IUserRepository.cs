using Dinder.Domain.Entities;
using Dinder.Domain.Enums;

namespace Dinder.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByExternalLoginAsync(ExternalProvider provider, string providerUserId, CancellationToken cancellationToken = default);
    Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    void Add(User user);
    void Update(User user);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
