using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Domain.Events;
using Dinder.Domain.Interfaces;
using Dinder.Application.Notifications.Handlers;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;

namespace Dinder.UnitTests;

public class IcebreakerTests
{
    // ── Entity: Conversation Icebreaker ─────────────────────────────────

    [Fact]
    public void Conversation_AssignIcebreaker_SetsQuestionAndCategory()
    {
        var conversation = new Conversation(Guid.NewGuid());

        conversation.AssignIcebreaker("What's your go-to karaoke song?", IcebreakerCategory.Funny);

        Assert.Equal("What's your go-to karaoke song?", conversation.IcebreakerQuestion);
        Assert.Equal(IcebreakerCategory.Funny, conversation.IcebreakerCategory);
    }

    [Fact]
    public void Conversation_NewConversation_HasNoIcebreaker()
    {
        var conversation = new Conversation(Guid.NewGuid());

        Assert.Null(conversation.IcebreakerQuestion);
        Assert.Null(conversation.IcebreakerCategory);
    }

    // ── Handler: AssignIcebreaker ───────────────────────────────────────

    [Fact]
    public async Task AssignIcebreaker_WithEnabledQuestions_AssignsOne()
    {
        // Arrange
        var matchId = Guid.NewGuid();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var conversation = new Conversation(matchId);
        typeof(Conversation).GetProperty(nameof(Conversation.Id))!.SetValue(conversation, conversationId);

        var icebreakers = new List<IcebreakerLibrary>
        {
            new("Funny question 1", IcebreakerCategory.Funny),
            new("Deep question 1", IcebreakerCategory.Deep),
            new("Dating question 1", IcebreakerCategory.Dating),
        };
        for (int i = 0; i < icebreakers.Count; i++)
            typeof(IcebreakerLibrary).GetProperty(nameof(IcebreakerLibrary.Id))!.SetValue(icebreakers[i], Guid.NewGuid());

        var adminRepoMock = new Mock<IAdminRepository>();
        adminRepoMock.Setup(r => r.GetEnabledIcebreakerLibraryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(icebreakers);

        var chatRepoMock = new Mock<IChatRepository>();
        chatRepoMock.Setup(r => r.GetConversationByMatchIdAsync(matchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var logger = NullLogger<AssignIcebreakerHandler>.Instance;
        var handler = new AssignIcebreakerHandler(adminRepoMock.Object, chatRepoMock.Object, logger);

        var notification = new MatchCreatedEvent(matchId, userId1, userId2);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        Assert.NotNull(conversation.IcebreakerQuestion);
        Assert.NotNull(conversation.IcebreakerCategory);
        chatRepoMock.Verify(r => r.UpdateConversation(conversation), Times.Once);
        chatRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AssignIcebreaker_NoEnabledQuestions_DoesNotAssign()
    {
        // Arrange
        var matchId = Guid.NewGuid();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var conversation = new Conversation(matchId);

        var adminRepoMock = new Mock<IAdminRepository>();
        adminRepoMock.Setup(r => r.GetEnabledIcebreakerLibraryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IcebreakerLibrary>());

        var chatRepoMock = new Mock<IChatRepository>();
        var logger = NullLogger<AssignIcebreakerHandler>.Instance;
        var handler = new AssignIcebreakerHandler(adminRepoMock.Object, chatRepoMock.Object, logger);

        var notification = new MatchCreatedEvent(matchId, userId1, userId2);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        Assert.Null(conversation.IcebreakerQuestion);
        chatRepoMock.Verify(r => r.UpdateConversation(It.IsAny<Conversation>()), Times.Never);
    }

    [Fact]
    public async Task AssignIcebreaker_ConversationNotFound_DoesNotThrow()
    {
        // Arrange
        var matchId = Guid.NewGuid();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        var icebreakers = new List<IcebreakerLibrary>
        {
            new("A question", IcebreakerCategory.Funny),
        };
        typeof(IcebreakerLibrary).GetProperty(nameof(IcebreakerLibrary.Id))!.SetValue(icebreakers[0], Guid.NewGuid());

        var adminRepoMock = new Mock<IAdminRepository>();
        adminRepoMock.Setup(r => r.GetEnabledIcebreakerLibraryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(icebreakers);

        var chatRepoMock = new Mock<IChatRepository>();
        chatRepoMock.Setup(r => r.GetConversationByMatchIdAsync(matchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Conversation?)null);

        var logger = NullLogger<AssignIcebreakerHandler>.Instance;
        var handler = new AssignIcebreakerHandler(adminRepoMock.Object, chatRepoMock.Object, logger);

        var notification = new MatchCreatedEvent(matchId, userId1, userId2);

        // Act — should NOT throw (fire-and-forget catches internally)
        await handler.Handle(notification, CancellationToken.None);

        // Assert — no update attempted
        chatRepoMock.Verify(r => r.UpdateConversation(It.IsAny<Conversation>()), Times.Never);
    }

    [Fact]
    public async Task AssignIcebreaker_RepositoryThrows_DoesNotPropagate()
    {
        // Arrange
        var matchId = Guid.NewGuid();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        var adminRepoMock = new Mock<IAdminRepository>();
        adminRepoMock.Setup(r => r.GetEnabledIcebreakerLibraryAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB down"));

        var chatRepoMock = new Mock<IChatRepository>();
        var logger = NullLogger<AssignIcebreakerHandler>.Instance;
        var handler = new AssignIcebreakerHandler(adminRepoMock.Object, chatRepoMock.Object, logger);

        var notification = new MatchCreatedEvent(matchId, userId1, userId2);

        // Act — fire-and-forget must NOT throw
        await handler.Handle(notification, CancellationToken.None);

        // No exception means the test passes
    }

    [Fact]
    public async Task AssignIcebreaker_SingleQuestion_AlwaysAssignsIt()
    {
        // Arrange
        var matchId = Guid.NewGuid();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var conversation = new Conversation(matchId);
        typeof(Conversation).GetProperty(nameof(Conversation.Id))!.SetValue(conversation, conversationId);

        var single = new IcebreakerLibrary("The only question", IcebreakerCategory.Dating);
        typeof(IcebreakerLibrary).GetProperty(nameof(IcebreakerLibrary.Id))!.SetValue(single, Guid.NewGuid());

        var adminRepoMock = new Mock<IAdminRepository>();
        adminRepoMock.Setup(r => r.GetEnabledIcebreakerLibraryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IcebreakerLibrary> { single });

        var chatRepoMock = new Mock<IChatRepository>();
        chatRepoMock.Setup(r => r.GetConversationByMatchIdAsync(matchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        var logger = NullLogger<AssignIcebreakerHandler>.Instance;
        var handler = new AssignIcebreakerHandler(adminRepoMock.Object, chatRepoMock.Object, logger);

        var notification = new MatchCreatedEvent(matchId, userId1, userId2);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        Assert.Equal("The only question", conversation.IcebreakerQuestion);
        Assert.Equal(IcebreakerCategory.Dating, conversation.IcebreakerCategory);
    }

    // ── Entity: IcebreakerLibrary ───────────────────────────────────────

    [Fact]
    public void IcebreakerLibrary_Constructor_SetsFields()
    {
        var icebreaker = new IcebreakerLibrary("What's your hidden talent?", IcebreakerCategory.Deep);

        Assert.NotEqual(Guid.Empty, icebreaker.Id);
        Assert.Equal("What's your hidden talent?", icebreaker.Text);
        Assert.Equal(IcebreakerCategory.Deep, icebreaker.Category);
        Assert.True(icebreaker.IsEnabled);
        Assert.True(icebreaker.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void IcebreakerLibrary_EnableDisable_Works()
    {
        var icebreaker = new IcebreakerLibrary("Test question", IcebreakerCategory.Funny);

        icebreaker.Disable();
        Assert.False(icebreaker.IsEnabled);

        icebreaker.Enable();
        Assert.True(icebreaker.IsEnabled);
    }
}
