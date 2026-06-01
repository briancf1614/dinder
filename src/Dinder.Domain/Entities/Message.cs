namespace Dinder.Domain.Entities;

public sealed class Message
{
    public Guid Id { get; private set; }
    public Guid ConversationId { get; private set; }
    public Guid SenderId { get; private set; }
    public string Content { get; private set; }
    public DateTime SentAt { get; private set; }
    public DateTime? ReadAt { get; private set; }

#pragma warning disable CS8618
    private Message() { } // EF Core
#pragma warning restore CS8618

    public Message(Guid conversationId, Guid senderId, string content)
    {
        Id = Guid.NewGuid();
        ConversationId = conversationId;
        SenderId = senderId;
        Content = content;
        SentAt = DateTime.UtcNow;
    }

    public void MarkRead()
    {
        ReadAt = DateTime.UtcNow;
    }
}
