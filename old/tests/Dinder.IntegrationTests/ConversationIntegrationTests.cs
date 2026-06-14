using Dinder.Application.Chat.Queries;
using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Domain.Interfaces;
using Moq;
using Xunit;
using MatchEntity = Dinder.Domain.Entities.Match;

namespace Dinder.IntegrationTests;

/// <summary>
/// Application-layer integration tests for the conversation list query.
/// Tests the full handler pipeline including repository orchestration.
/// For HTTP-level tests, see the ChatController with WebApplicationFactory
/// (requires running PostgreSQL via Testcontainers/docker-compose).
/// </summary>
public class ConversationIntegrationTests
{
    private readonly Mock<IChatRepository> _chatRepoMock;
    private readonly Mock<IProfileRepository> _profileRepoMock;
    private readonly GetConversationsQueryHandler _handler;

    public ConversationIntegrationTests()
    {
        _chatRepoMock = new Mock<IChatRepository>();
        _profileRepoMock = new Mock<IProfileRepository>();
        _handler = new GetConversationsQueryHandler(_chatRepoMock.Object, _profileRepoMock.Object);
    }

    private (Conversation conversation, Guid otherUserId) CreateTestConversation(
        Guid userId,
        string displayName = "Test User",
        string? icebreakerQuestion = null,
        IcebreakerCategory? icebreakerCategory = null)
    {
        var otherUserId = Guid.NewGuid();
        var match = new MatchEntity(userId, otherUserId);
        typeof(MatchEntity).GetProperty(nameof(MatchEntity.Id))!.SetValue(match, Guid.NewGuid());

        var conversation = new Conversation(match.Id);
        typeof(Conversation).GetProperty(nameof(Conversation.Id))!
            .SetValue(conversation, Guid.NewGuid());
        typeof(Conversation).GetProperty(nameof(Conversation.Match))!
            .SetValue(conversation, match);

        if (icebreakerQuestion is not null && icebreakerCategory is not null)
            conversation.AssignIcebreaker(icebreakerQuestion, icebreakerCategory.Value);

        _profileRepoMock.Setup(r => r.GetByUserIdAsync(otherUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Profile(otherUserId, displayName, Gender.Other,
                DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25))));

        _chatRepoMock.Setup(r => r.GetUnreadMessageCountAsync(
                conversation.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _chatRepoMock.Setup(r => r.GetMessagesAsync(
                conversation.Id, null, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        return (conversation, otherUserId);
    }

    /// <summary>RC-6: 200 with icebreaker data.</summary>
    [Fact]
    public async Task GetConversations_IcebreakerData_ReturnsConversationWithIcebreaker()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var (conversation, _) = CreateTestConversation(
            userId, "Alice",
            icebreakerQuestion: "What's your favorite travel spot?",
            icebreakerCategory: IcebreakerCategory.Lifestyle);

        _chatRepoMock.Setup(r => r.GetConversationsByUserIdAsync(
                userId, null, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync([conversation]);

        // Act
        var result = await _handler.Handle(
            new GetConversationsQuery(userId), CancellationToken.None);

        // Assert
        Assert.Single(result.Conversations);
        var dto = result.Conversations[0];
        Assert.Equal("What's your favorite travel spot?", dto.IcebreakerQuestion);
        Assert.Equal("Lifestyle", dto.IcebreakerCategory);
    }

    /// <summary>RC-6: 200 empty list for new user with no matches.</summary>
    [Fact]
    public async Task GetConversations_NewUser_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _chatRepoMock.Setup(r => r.GetConversationsByUserIdAsync(
                userId, null, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _handler.Handle(
            new GetConversationsQuery(userId), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Conversations);
        Assert.Null(result.NextCursor);
    }

    /// <summary>RC-6: Unmatched conversations excluded.</summary>
    [Fact]
    public async Task GetConversations_UnmatchedExcluded_DoesNotAppear()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var (activeConversation, activeOtherId) = CreateTestConversation(userId, "ActiveMatch");

        // Create an unmatched conversation
        var unmatchedMatch = new MatchEntity(userId, Guid.NewGuid());
        typeof(MatchEntity).GetProperty(nameof(MatchEntity.Id))!
            .SetValue(unmatchedMatch, Guid.NewGuid());
        var unmatchedConversation = new Conversation(unmatchedMatch.Id);
        typeof(Conversation).GetProperty(nameof(Conversation.Id))!
            .SetValue(unmatchedConversation, Guid.NewGuid());
        typeof(Conversation).GetProperty(nameof(Conversation.Match))!
            .SetValue(unmatchedConversation, unmatchedMatch);
        unmatchedConversation.Unmatch(userId);

        // Repository returns only active (filtering is done by repository)
        _chatRepoMock.Setup(r => r.GetConversationsByUserIdAsync(
                userId, null, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync([activeConversation]);

        // Act
        var result = await _handler.Handle(
            new GetConversationsQuery(userId), CancellationToken.None);

        // Assert
        Assert.Single(result.Conversations);
        Assert.Equal("ActiveMatch", result.Conversations[0].DisplayName);
    }

    /// <summary>Verifies pagination cursor behavior with exactly limit+1 results.</summary>
    [Fact]
    public async Task GetConversations_Pagination_ReturnsNextCursorWhenMoreExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var conversations = new List<Conversation>();

        for (int i = 0; i < 21; i++)
        {
            var (conv, _) = CreateTestConversation(userId, $"User {i}");
            conversations.Add(conv);
        }

        _chatRepoMock.Setup(r => r.GetConversationsByUserIdAsync(
                userId, null, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversations);

        // Act
        var result = await _handler.Handle(
            new GetConversationsQuery(userId), CancellationToken.None);

        // Assert
        Assert.Equal(20, result.Conversations.Count);
        Assert.NotNull(result.NextCursor);
    }
}
