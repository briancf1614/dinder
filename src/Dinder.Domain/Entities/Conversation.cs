using Dinder.Domain.Enums;

namespace Dinder.Domain.Entities;

public sealed class Conversation
{
    public Guid Id { get; private set; }
    public Guid MatchId { get; private set; }
    public ConversationStatus Status { get; private set; }
    public Guid? UnmatchedByUserId { get; private set; }
    public DateTime? UnmatchedAt { get; private set; }
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
        Status = ConversationStatus.Active;
        CreatedAt = DateTime.UtcNow;
    }

    public void Unmatch(Guid unmatchingUserId)
    {
        Status = ConversationStatus.Unmatched;
        UnmatchedByUserId = unmatchingUserId;
        UnmatchedAt = DateTime.UtcNow;
    }

    public bool CanSendMessages() => Status == ConversationStatus.Active;

    public bool IsParticipant(Guid userId, Guid userId1, Guid userId2) =>
        userId == userId1 || userId == userId2;
}
