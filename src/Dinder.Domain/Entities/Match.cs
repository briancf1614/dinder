namespace Dinder.Domain.Entities;

public sealed class Match
{
    public Guid Id { get; private set; }
    public Guid UserId1 { get; private set; }
    public Guid UserId2 { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public Conversation? Conversation { get; private set; }

#pragma warning disable CS8618
    private Match() { } // EF Core
#pragma warning restore CS8618

    public Match(Guid userId1, Guid userId2)
    {
        Id = Guid.NewGuid();
        UserId1 = userId1;
        UserId2 = userId2;
        CreatedAt = DateTime.UtcNow;
    }
}
