namespace Dinder.Domain.Entities;

public sealed class Block
{
    public Guid Id { get; private set; }
    public Guid BlockerId { get; private set; }
    public Guid BlockedId { get; private set; }
    public DateTime CreatedAt { get; private set; }

#pragma warning disable CS8618
    private Block() { } // EF Core
#pragma warning restore CS8618

    public Block(Guid blockerId, Guid blockedId)
    {
        Id = Guid.NewGuid();
        BlockerId = blockerId;
        BlockedId = blockedId;
        CreatedAt = DateTime.UtcNow;
    }
}
