using MediatR;

namespace Dinder.Domain.Events;

public sealed record MessageSentEvent(Guid MessageId, Guid ConversationId, Guid SenderId, Guid RecipientId, string ContentPreview) : INotification;
