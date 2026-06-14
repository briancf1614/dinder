using Dinder.Domain.Enums;

namespace Dinder.Domain.Entities;

public sealed class UserExternalLogin
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public ExternalProvider Provider { get; private set; }
    public string ProviderUserId { get; private set; }

    // Navigation
    public User User { get; private set; } = null!;

#pragma warning disable CS8618
    private UserExternalLogin() { } // EF Core
#pragma warning restore CS8618

    public UserExternalLogin(Guid userId, ExternalProvider provider, string providerUserId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Provider = provider;
        ProviderUserId = providerUserId;
    }
}
