using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Dinder.Infrastructure.Persistence;

public sealed class UserRepository : IUserRepository
{
    private readonly DinderDbContext _context;

    public UserRepository(DinderDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.ExternalLogins)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.ToLowerInvariant();
        return await _context.Users
            .Include(u => u.ExternalLogins)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => EF.Property<string>(u, "Email") == normalized, cancellationToken);
    }

    public async Task<User?> GetByExternalLoginAsync(
        ExternalProvider provider,
        string providerUserId,
        CancellationToken cancellationToken = default)
    {
        return await _context.UserExternalLogins
            .Where(x => x.Provider == provider && x.ProviderUserId == providerUserId)
            .Include(x => x.User)
                .ThenInclude(u => u.RefreshTokens)
            .Include(x => x.User)
                .ThenInclude(u => u.ExternalLogins)
            .Select(x => x.User)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .Where(rt => rt.Token == refreshToken)
            .Include(rt => rt.User)
                .ThenInclude(u => u.RefreshTokens)
            .Include(rt => rt.User)
                .ThenInclude(u => u.ExternalLogins)
            .Select(rt => rt.User)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.ToLowerInvariant();
        return await _context.Users.AnyAsync(u => EF.Property<string>(u, "Email") == normalized, cancellationToken);
    }

    public void Add(User user) => _context.Users.Add(user);

    public void Update(User user) => _context.Users.Update(user);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
