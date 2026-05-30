namespace Dinder.Domain.Entities;

public sealed class Conversation
{
    public Guid Id { get; private set; }
    public Guid MatchId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public Match Match { get; private set; } = null!;

#pragma warning disable CS8618
    private Conversation() { } // EF Core
#pragma warning restore CS8618

    public Conversation(Guid matchId)
    {
        Id = Guid.NewGuid();
        MatchId = matchId;
        CreatedAt = DateTime.UtcNow;
    }
}
