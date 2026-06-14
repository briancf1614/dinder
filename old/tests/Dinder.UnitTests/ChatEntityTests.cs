using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Xunit;

namespace Dinder.UnitTests;

public class ChatEntityTests
{
    [Fact]
    public void Message_Constructor_CreatesWithCorrectValues()
    {
        var conversationId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var content = "Hello, world!";

        var message = new Message(conversationId, senderId, content);

        Assert.NotEqual(Guid.Empty, message.Id);
        Assert.Equal(conversationId, message.ConversationId);
        Assert.Equal(senderId, message.SenderId);
        Assert.Equal(content, message.Content);
        Assert.Null(message.ReadAt);
        Assert.True(message.SentAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Message_MarkRead_SetsReadAt()
    {
        var message = new Message(Guid.NewGuid(), Guid.NewGuid(), "test");

        Assert.Null(message.ReadAt);
        message.MarkRead();
        Assert.NotNull(message.ReadAt);
        Assert.True(message.ReadAt!.Value <= DateTime.UtcNow);
    }

    [Fact]
    public void Conversation_Constructor_CreatesActive()
    {
        var matchId = Guid.NewGuid();
        var conversation = new Conversation(matchId);

        Assert.Equal(matchId, conversation.MatchId);
        Assert.Equal(ConversationStatus.Active, conversation.Status);
        Assert.True(conversation.CanSendMessages());
    }

    [Fact]
    public void Conversation_Unmatch_SetsUnmatchedState()
    {
        var conversation = new Conversation(Guid.NewGuid());
        var unmatchingUserId = Guid.NewGuid();

        conversation.Unmatch(unmatchingUserId);

        Assert.Equal(ConversationStatus.Unmatched, conversation.Status);
        Assert.Equal(unmatchingUserId, conversation.UnmatchedByUserId);
        Assert.NotNull(conversation.UnmatchedAt);
        Assert.False(conversation.CanSendMessages());
    }

    [Fact]
    public void Conversation_IsParticipant_ReturnsTrueForMatchUsers()
    {
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var conversation = new Conversation(Guid.NewGuid());

        Assert.True(conversation.IsParticipant(userId1, userId1, userId2));
        Assert.True(conversation.IsParticipant(userId2, userId1, userId2));
    }

    [Fact]
    public void Conversation_IsParticipant_ReturnsFalseForNonParticipants()
    {
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var conversation = new Conversation(Guid.NewGuid());

        Assert.False(conversation.IsParticipant(otherUserId, userId1, userId2));
    }

    [Fact]
    public void Conversation_CanSendMessages_ReturnsFalseAfterUnmatch()
    {
        var conversation = new Conversation(Guid.NewGuid());
        Assert.True(conversation.CanSendMessages());

        conversation.Unmatch(Guid.NewGuid());
        Assert.False(conversation.CanSendMessages());
    }
}
