using Dinder.Domain.Enums;
using Dinder.Domain.ValueObjects;

namespace Dinder.Domain.Entities;

public sealed class User
{
    public Guid Id { get; private set; }
    public Email Email { get; private set; }
    public string PasswordHash { get; private set; }
    public AccountStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? SoftDeletedAt { get; private set; }
    public string? BanReason { get; private set; }

    private readonly List<UserExternalLogin> _externalLogins = [];
    public IReadOnlyCollection<UserExternalLogin> ExternalLogins => _externalLogins.AsReadOnly();

    private readonly List<RefreshToken> _refreshTokens = [];
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

#pragma warning disable CS8618
    private User() { } // EF Core
#pragma warning restore CS8618

    public User(Email email, string passwordHash)
    {
        Id = Guid.NewGuid();
        Email = email;
        PasswordHash = passwordHash;
        Status = AccountStatus.PendingVerification;
        CreatedAt = DateTime.UtcNow;
    }

    public static User CreateExternal(Email email, ExternalProvider provider, string providerUserId)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = string.Empty, // No password for external-only users
            Status = AccountStatus.Active, // External login users skip email verification
            CreatedAt = DateTime.UtcNow
        };
        user.AddExternalLogin(provider, providerUserId);
        return user;
    }

    public void AddExternalLogin(ExternalProvider provider, string providerUserId)
    {
        if (_externalLogins.Any(x => x.Provider == provider && x.ProviderUserId == providerUserId))
            return;
        _externalLogins.Add(new UserExternalLogin(Id, provider, providerUserId));
    }

    public RefreshToken AddRefreshToken(string token, DateTime expiresAt)
    {
        var refreshToken = new RefreshToken(Id, token, expiresAt);
        _refreshTokens.Add(refreshToken);
        return refreshToken;
    }

    public void RevokeAllRefreshTokens()
    {
        foreach (var rt in _refreshTokens.Where(x => !x.IsRevoked))
            rt.Revoke();
    }

    public void SoftDelete()
    {
        Status = AccountStatus.SoftDeleted;
        SoftDeletedAt = DateTime.UtcNow;
        RevokeAllRefreshTokens();
    }

    public void Ban(string reason)
    {
        Status = AccountStatus.Banned;
        BanReason = reason;
        RevokeAllRefreshTokens();
    }

    public void Unban()
    {
        Status = AccountStatus.Active;
        BanReason = null;
    }

    public bool CanAuthenticate() => Status == AccountStatus.Active;
}
