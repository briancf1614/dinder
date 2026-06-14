using Dinder.Domain.Enums;

namespace Dinder.Domain.Entities;

public sealed class Swipe
{
    public Guid Id { get; private set; }
    public Guid SwiperId { get; private set; }
    public Guid SwipedId { get; private set; }
    public SwipeDirection Direction { get; private set; }
    public DateTime CreatedAt { get; private set; }

#pragma warning disable CS8618
    private Swipe() { } // EF Core
#pragma warning restore CS8618

    public Swipe(Guid swiperId, Guid swipedId, SwipeDirection direction)
    {
        Id = Guid.NewGuid();
        SwiperId = swiperId;
        SwipedId = swipedId;
        Direction = direction;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateDirection(SwipeDirection direction)
    {
        Direction = direction;
        CreatedAt = DateTime.UtcNow;
    }
}
