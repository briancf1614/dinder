using Dinder.Domain.Entities;

namespace Dinder.Domain.Interfaces;

public interface IChatRepository
{
    // Messages
    Task<Message?> GetMessageAsync(Guid messageId, CancellationToken cancellationToken = default);
    void AddMessage(Message message);
    Task<List<Message>> GetMessagesAsync(Guid conversationId, Guid? cursor, int limit, CancellationToken cancellationToken = default);
    void MarkMessagesRead(Guid conversationId, Guid recipientId);

    // Conversation access
    Task<Conversation?> GetConversationAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task<Conversation?> GetConversationByMatchIdAsync(Guid matchId, CancellationToken cancellationToken = default);
    Task<bool> IsParticipantAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default);
    void UpdateConversation(Conversation conversation);

    // Unread count for notification badge
    Task<int> GetUnreadMessageCountAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
