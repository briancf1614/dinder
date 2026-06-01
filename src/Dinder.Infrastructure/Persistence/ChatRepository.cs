using Dinder.Domain.Entities;
using Dinder.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Dinder.Infrastructure.Persistence;

public sealed class ChatRepository : IChatRepository
{
    private readonly DiscoveryDbContext _discoveryContext;
    private readonly CommunicationDbContext _communicationContext;

    public ChatRepository(DiscoveryDbContext discoveryContext, CommunicationDbContext communicationContext)
    {
        _discoveryContext = discoveryContext;
        _communicationContext = communicationContext;
    }

    // ── Messages ────────────────────────────────────────────────────────

    public async Task<Message?> GetMessageAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        return await _communicationContext.Messages
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);
    }

    public void AddMessage(Message message)
    {
        _communicationContext.Messages.Add(message);
    }

    public async Task<List<Message>> GetMessagesAsync(Guid conversationId, Guid? cursor, int limit, CancellationToken cancellationToken = default)
    {
        var query = _communicationContext.Messages
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.SentAt) // Most recent first
            .ThenByDescending(m => m.Id);

        if (cursor.HasValue)
        {
            // Cursor-based: fetch messages older than the cursor
            var cursorMessage = await _communicationContext.Messages
                .FirstOrDefaultAsync(m => m.Id == cursor.Value, cancellationToken);
            if (cursorMessage is not null)
            {
                query = (IOrderedQueryable<Message>)query.Where(m =>
                    m.SentAt < cursorMessage.SentAt ||
                    (m.SentAt == cursorMessage.SentAt && m.Id.CompareTo(cursorMessage.Id) < 0));
            }
        }

        return await query.Take(limit).ToListAsync(cancellationToken);
    }

    public void MarkMessagesRead(Guid conversationId, Guid recipientId)
    {
        var unreadMessages = _communicationContext.Messages
            .Where(m => m.ConversationId == conversationId && m.SenderId != recipientId && m.ReadAt == null);

        foreach (var message in unreadMessages)
        {
            message.MarkRead();
        }
    }

    // ── Conversation ────────────────────────────────────────────────────

    public async Task<Conversation?> GetConversationAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        return await _discoveryContext.Conversations
            .Include(c => c.Match)
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);
    }

    public async Task<bool> IsParticipantAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var conversation = await _discoveryContext.Conversations
            .Include(c => c.Match)
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

        if (conversation is null)
            return false;

        return conversation.IsParticipant(userId, conversation.Match.UserId1, conversation.Match.UserId2);
    }

    public void UpdateConversation(Conversation conversation)
    {
        _discoveryContext.Conversations.Update(conversation);
    }

    // ── Unread count ────────────────────────────────────────────────────

    public async Task<int> GetUnreadMessageCountAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _communicationContext.Messages
            .CountAsync(m => m.ConversationId == conversationId && m.SenderId != userId && m.ReadAt == null, cancellationToken);
    }

    // ── Save ────────────────────────────────────────────────────────────

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _discoveryContext.SaveChangesAsync(cancellationToken);
        await _communicationContext.SaveChangesAsync(cancellationToken);
    }
}
