using Dinder.Application.Chat.Commands;
using Dinder.Application.Chat.Queries;
using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Domain.Events;
using Dinder.Domain.Interfaces;
using MediatR;
using Moq;
using Xunit;
using MatchEntity = Dinder.Domain.Entities.Match;

namespace Dinder.UnitTests;

public class ChatHandlerTests
{
    private readonly Mock<IChatRepository> _chatRepoMock;
    private readonly Mock<IMediator> _mediatorMock;

    public ChatHandlerTests()
    {
        _chatRepoMock = new Mock<IChatRepository>();
        _mediatorMock = new Mock<IMediator>();
    }

    [Fact]
    public async Task SendMessage_PersistsAndReturnsMessage()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        var matchId = Guid.NewGuid();

        var match = new MatchEntity(senderId, recipientId);
        typeof(MatchEntity).GetProperty(nameof(MatchEntity.Id))!.SetValue(match, matchId);

        var conversation = new Conversation(matchId);
        typeof(Conversation).GetProperty(nameof(Conversation.Id))!.SetValue(conversation, conversationId);
        typeof(Conversation).GetProperty(nameof(Conversation.Match))!.SetValue(conversation, match);

        _chatRepoMock.Setup(r => r.GetConversationAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        _chatRepoMock.Setup(r => r.IsParticipantAsync(conversationId, senderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new SendMessageCommandHandler(_chatRepoMock.Object, _mediatorMock.Object);
        var command = new SendMessageCommand(conversationId, senderId, "Hello!");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Hello!", result.Content);
        Assert.Equal(senderId, result.SenderId);
        Assert.Equal(conversationId, result.ConversationId);

        _chatRepoMock.Verify(r => r.AddMessage(It.IsAny<Message>()), Times.Once);
        _chatRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mediatorMock.Verify(m => m.Publish(It.Is<MessageSentEvent>(e =>
            e.ConversationId == conversationId && e.SenderId == senderId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendMessage_NonParticipant_ThrowsUnauthorized()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var match = new MatchEntity(Guid.NewGuid(), Guid.NewGuid());
        var conversation = new Conversation(match.Id);
        typeof(Conversation).GetProperty(nameof(Conversation.Id))!.SetValue(conversation, conversationId);
        typeof(Conversation).GetProperty(nameof(Conversation.Match))!.SetValue(conversation, match);

        _chatRepoMock.Setup(r => r.GetConversationAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        _chatRepoMock.Setup(r => r.IsParticipantAsync(conversationId, senderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new SendMessageCommandHandler(_chatRepoMock.Object, _mediatorMock.Object);
        var command = new SendMessageCommand(conversationId, senderId, "Hello!");

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task SendMessage_UnmatchedConversation_Throws()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var match = new MatchEntity(Guid.NewGuid(), Guid.NewGuid());
        var conversation = new Conversation(match.Id);
        typeof(Conversation).GetProperty(nameof(Conversation.Id))!.SetValue(conversation, conversationId);
        typeof(Conversation).GetProperty(nameof(Conversation.Match))!.SetValue(conversation, match);
        conversation.Unmatch(Guid.NewGuid());

        _chatRepoMock.Setup(r => r.GetConversationAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var handler = new SendMessageCommandHandler(_chatRepoMock.Object, _mediatorMock.Object);
        var command = new SendMessageCommand(conversationId, senderId, "Hello!");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal("CONVERSATION_UNMATCHED", ex.Message);
    }

    [Fact]
    public async Task Unmatch_SuccessfullyUnmatches()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var match = new MatchEntity(userId, Guid.NewGuid());
        var conversation = new Conversation(match.Id);
        typeof(Conversation).GetProperty(nameof(Conversation.Id))!.SetValue(conversation, conversationId);
        typeof(Conversation).GetProperty(nameof(Conversation.Match))!.SetValue(conversation, match);

        _chatRepoMock.Setup(r => r.GetConversationAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var handler = new UnmatchCommandHandler(_chatRepoMock.Object);
        var command = new UnmatchCommand(conversationId, userId);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ConversationStatus.Unmatched, conversation.Status);
        Assert.Equal(userId, conversation.UnmatchedByUserId);
        _chatRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMessages_Participant_ReturnsMessages()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var match = new MatchEntity(userId, Guid.NewGuid());
        var conversation = new Conversation(match.Id);
        typeof(Conversation).GetProperty(nameof(Conversation.Id))!.SetValue(conversation, conversationId);
        typeof(Conversation).GetProperty(nameof(Conversation.Match))!.SetValue(conversation, match);

        var messages = new List<Message>
        {
            new(conversationId, userId, "First message"),
            new(conversationId, Guid.NewGuid(), "Second message"),
        };

        _chatRepoMock.Setup(r => r.IsParticipantAsync(conversationId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _chatRepoMock.Setup(r => r.GetConversationAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);
        _chatRepoMock.Setup(r => r.GetMessagesAsync(conversationId, null, 51, It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

        var handler = new GetMessagesQueryHandler(_chatRepoMock.Object);
        var query = new GetMessagesQuery(conversationId, userId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Messages.Count);
        Assert.Null(result.NextCursor); // No more pages
    }
}
