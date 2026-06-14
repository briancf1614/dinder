using System.Security.Claims;
using Dinder.Application.Chat.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dinder.Infrastructure.SignalR;

[Authorize]
public sealed class ChatHub : Hub
{
    private readonly IMediator _mediator;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(IMediator mediator, ILogger<ChatHub> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>Send a message to the conversation. Persists before acknowledging.</summary>
    public async Task SendMessage(Guid conversationId, string content)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            await Clients.Caller.SendAsync("Error", "Not authenticated.");
            return;
        }

        try
        {
            var result = await _mediator.Send(new SendMessageCommand(conversationId, userId.Value, content));

            // Broadcast to all users in the conversation group (both sender and recipient)
            await Clients.Group(GetGroupName(conversationId))
                .SendAsync("ReceiveMessage", new
                {
                    result.MessageId,
                    result.ConversationId,
                    result.SenderId,
                    result.Content,
                    result.SentAt
                });

            _logger.LogDebug("Message {MessageId} sent in conversation {ConversationId} by {SenderId}",
                result.MessageId, conversationId, userId);
        }
        catch (UnauthorizedAccessException ex)
        {
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    /// <summary>Notify the other user that the current user is typing.</summary>
    public async Task TypingIndicator(Guid conversationId, bool isTyping)
    {
        var userId = GetUserId();
        if (userId is null) return;

        // Broadcast typing status to other users in the group
        await Clients.OthersInGroup(GetGroupName(conversationId))
            .SendAsync("TypingUpdate", new { userId = userId.Value, conversationId, isTyping });
    }

    /// <summary>Mark all messages in a conversation as read by the current user.</summary>
    public async Task MarkRead(Guid conversationId)
    {
        var userId = GetUserId();
        if (userId is null) return;

        try
        {
            // Use MediatR or direct repository to mark messages read
            // For simplicity and direct hub access, we use the existing chat logic
            var handler = Context.GetHttpContext()?.RequestServices.GetRequiredService<Domain.Interfaces.IChatRepository>();
            if (handler is not null)
            {
                handler.MarkMessagesRead(conversationId, userId.Value);
                await handler.SaveChangesAsync();

                // Notify other user in the conversation that messages were read
                await Clients.OthersInGroup(GetGroupName(conversationId))
                    .SendAsync("MessageRead", new { conversationId, readByUserId = userId.Value });
            }

            _logger.LogDebug("Messages in conversation {ConversationId} marked read by {UserId}",
                conversationId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking messages as read in conversation {ConversationId}", conversationId);
        }
    }

    /// <summary>Join a conversation group. Called by client after connection established.</summary>
    public async Task JoinConversation(Guid conversationId)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            await Clients.Caller.SendAsync("Error", "Not authenticated.");
            return;
        }

        // Verify participant access before joining group (match-gated)
        var chatRepo = Context.GetHttpContext()?.RequestServices.GetRequiredService<Domain.Interfaces.IChatRepository>();
        if (chatRepo is not null)
        {
            var isParticipant = await chatRepo.IsParticipantAsync(conversationId, userId.Value);
            if (!isParticipant)
            {
                await Clients.Caller.SendAsync("Error", "NOT_PARTICIPANT");
                return;
            }
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(conversationId));

        // Track user presence in the conversation
        _logger.LogDebug("User {UserId} joined conversation {ConversationId}", userId, conversationId);
    }

    /// <summary>Leave a conversation group.</summary>
    public async Task LeaveConversation(Guid conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(conversationId));
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        _logger.LogDebug("User {UserId} connected to ChatHub (Connection: {ConnectionId})",
            userId, Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        _logger.LogDebug("User {UserId} disconnected from ChatHub (Connection: {ConnectionId})",
            userId, Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    private Guid? GetUserId()
    {
        var userIdClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirstValue("sub");

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private static string GetGroupName(Guid conversationId) => $"conversation_{conversationId}";
}
