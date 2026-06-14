using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Xunit;

namespace Dinder.UnitTests;

public class SwipeTests
{
    [Fact]
    public void Constructor_CreatesSwipe_WithCorrectValues()
    {
        var swiperId = Guid.NewGuid();
        var swipedId = Guid.NewGuid();
        var swipe = new Swipe(swiperId, swipedId, SwipeDirection.Right);

        Assert.Equal(swiperId, swipe.SwiperId);
        Assert.Equal(swipedId, swipe.SwipedId);
        Assert.Equal(SwipeDirection.Right, swipe.Direction);
        Assert.NotEqual(Guid.Empty, swipe.Id);
    }

    [Fact]
    public void UpdateDirection_ChangesDirectionAndTimestamp()
    {
        var swipe = new Swipe(Guid.NewGuid(), Guid.NewGuid(), SwipeDirection.Left);
        var originalTime = swipe.CreatedAt;

        swipe.UpdateDirection(SwipeDirection.Right);

        Assert.Equal(SwipeDirection.Right, swipe.Direction);
        Assert.True(swipe.CreatedAt >= originalTime);
    }

    [Fact]
    public void Match_CreatesWithBothUserIds()
    {
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var match = new Match(userId1, userId2);

        Assert.Equal(userId1, match.UserId1);
        Assert.Equal(userId2, match.UserId2);
        Assert.NotEqual(Guid.Empty, match.Id);
    }

    [Fact]
    public void Conversation_CreatesWithMatchId()
    {
        var matchId = Guid.NewGuid();
        var conversation = new Conversation(matchId);

        Assert.Equal(matchId, conversation.MatchId);
        Assert.NotEqual(Guid.Empty, conversation.Id);
    }
}
