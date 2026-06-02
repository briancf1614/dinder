using Dinder.Application.Chat.Queries;
using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Domain.Interfaces;
using Moq;
using Xunit;
using MatchEntity = Dinder.Domain.Entities.Match;

namespace Dinder.UnitTests;

public class ConversationQueryTests
{
    private readonly Mock<IChatRepository> _chatRepoMock;
    private readonly Mock<IProfileRepository> _profileRepoMock;

    public ConversationQueryTests()
    {
        _chatRepoMock = new Mock<IChatRepository>();
        _profileRepoMock = new Mock<IProfileRepository>();
    }

    private static Conversation CreateConversation(
        Guid conversationId, Guid userId1, Guid userId2,
        DateTime createdAt, string? icebreakerQuestion = null,
        IcebreakerCategory? icebreakerCategory = null)
    {
        var match = new MatchEntity(userId1, userId2);
        typeof(MatchEntity).GetProperty(nameof(MatchEntity.Id))!.SetValue(match, Guid.NewGuid());

        var conversation = new Conversation(match.Id);
        typeof(Conversation).GetProperty(nameof(Conversation.Id))!.SetValue(conversation, conversationId);
        typeof(Conversation).GetProperty(nameof(Conversation.CreatedAt))!.SetValue(conversation, createdAt);
        typeof(Conversation).GetProperty(nameof(Conversation.Match))!.SetValue(conversation, match);

        if (icebreakerQuestion is not null && icebreakerCategory is not null)
            conversation.AssignIcebreaker(icebreakerQuestion, icebreakerCategory.Value);

        return conversation;
    }

    [Fact]
    public async Task GetConversations_ReturnsPaginatedList_WithNextCursor()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var matchId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var conversations = new List<Conversation>();
        for (int i = 0; i < 21; i++) // 21 = limit (20) + 1 extra to trigger hasMore
        {
            conversations.Add(CreateConversation(
                Guid.NewGuid(), userId, Guid.NewGuid(),
                now.AddMinutes(-i)));
        }

        _chatRepoMock.Setup(r => r.GetConversationsByUserIdAsync(
                userId, null, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversations);

        // Set up profile lookups for each other user
        foreach (var c in conversations)
        {
            var otherId = c.Match.UserId1 == userId ? c.Match.UserId2 : c.Match.UserId1;
            _profileRepoMock.Setup(r => r.GetByUserIdAsync(otherId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Profile(otherId, "Test User", Gender.Other, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25))));
            _chatRepoMock.Setup(r => r.GetUnreadMessageCountAsync(c.Id, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);
            _chatRepoMock.Setup(r => r.GetMessagesAsync(c.Id, null, 1, It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
        }

        var handler = new GetConversationsQueryHandler(_chatRepoMock.Object, _profileRepoMock.Object);
        var query = new GetConversationsQuery(userId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(20, result.Conversations.Count);
        Assert.NotNull(result.NextCursor);
    }

    [Fact]
    public async Task GetConversations_EmptyList_ReturnsEmptyWithNoCursor()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _chatRepoMock.Setup(r => r.GetConversationsByUserIdAsync(
                userId, null, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = new GetConversationsQueryHandler(_chatRepoMock.Object, _profileRepoMock.Object);
        var query = new GetConversationsQuery(userId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Conversations);
        Assert.Null(result.NextCursor);
    }

    [Fact]
    public async Task GetConversations_IncludesIcebreakerData()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var icebreakerQuestion = "What's your favorite travel destination?";
        var icebreakerCategory = IcebreakerCategory.Deep;

        var conversation = CreateConversation(
            conversationId, userId, otherId,
            DateTime.UtcNow, icebreakerQuestion, icebreakerCategory);

        _chatRepoMock.Setup(r => r.GetConversationsByUserIdAsync(
                userId, null, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync([conversation]);

        _profileRepoMock.Setup(r => r.GetByUserIdAsync(otherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Profile(otherId, "Alice", Gender.Female, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25))));
        _chatRepoMock.Setup(r => r.GetUnreadMessageCountAsync(conversationId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        _chatRepoMock.Setup(r => r.GetMessagesAsync(conversationId, null, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Message(conversationId, otherId, "Hey there!")]);

        var handler = new GetConversationsQueryHandler(_chatRepoMock.Object, _profileRepoMock.Object);
        var query = new GetConversationsQuery(userId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Single(result.Conversations);
        var dto = result.Conversations[0];
        Assert.Equal(conversationId, dto.ConversationId);
        Assert.Equal("Alice", dto.DisplayName);
        Assert.Equal("Hey there!", dto.LastMessage);
        Assert.Equal(3, dto.UnreadCount);
        Assert.Equal(icebreakerQuestion, dto.IcebreakerQuestion);
        Assert.Equal(icebreakerCategory.ToString(), dto.IcebreakerCategory);
    }

    [Fact]
    public async Task GetConversations_CursorPagination_ReturnsNextPage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var cursorConversationId = Guid.NewGuid();

        // Page 1 conversations (newest)
        var page1 = new List<Conversation>
        {
            CreateConversation(Guid.NewGuid(), userId, Guid.NewGuid(), now),
            CreateConversation(Guid.NewGuid(), userId, Guid.NewGuid(), now.AddMinutes(-1)),
            CreateConversation(cursorConversationId, userId, Guid.NewGuid(), now.AddMinutes(-2)),
        };

        // Page 2 conversations (older) — query with cursor should get these
        var page2 = new List<Conversation>
        {
            CreateConversation(Guid.NewGuid(), userId, Guid.NewGuid(), now.AddMinutes(-3)),
            CreateConversation(Guid.NewGuid(), userId, Guid.NewGuid(), now.AddMinutes(-4)),
        };

        _chatRepoMock.Setup(r => r.GetConversationsByUserIdAsync(
                userId, cursorConversationId, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page2);

        foreach (var c in page2)
        {
            var otherId = c.Match.UserId1 == userId ? c.Match.UserId2 : c.Match.UserId1;
            _profileRepoMock.Setup(r => r.GetByUserIdAsync(otherId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Profile(otherId, "Test", Gender.Other, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25))));
            _chatRepoMock.Setup(r => r.GetUnreadMessageCountAsync(c.Id, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);
            _chatRepoMock.Setup(r => r.GetMessagesAsync(c.Id, null, 1, It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
        }

        var handler = new GetConversationsQueryHandler(_chatRepoMock.Object, _profileRepoMock.Object);
        var query = new GetConversationsQuery(userId, cursorConversationId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Conversations.Count);
        Assert.Null(result.NextCursor); // No more pages
    }
}
