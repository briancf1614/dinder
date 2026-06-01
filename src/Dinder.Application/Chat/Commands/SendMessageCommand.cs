using Dinder.Domain.Entities;
using Dinder.Domain.Events;
using Dinder.Domain.Interfaces;
using MediatR;

namespace Dinder.Application.Chat.Commands;

public sealed record SendMessageCommand(Guid ConversationId, Guid SenderId, string Content) : IRequest<SendMessageResult>;

public sealed record SendMessageResult(
    Guid MessageId,
    Guid ConversationId,
    Guid SenderId,
    string Content,
    DateTime SentAt);

public sealed class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, SendMessageResult>
{
    private readonly IChatRepository _chatRepository;
    private readonly IMediator _mediator;

    public SendMessageCommandHandler(IChatRepository chatRepository, IMediator mediator)
    {
        _chatRepository = chatRepository;
        _mediator = mediator;
    }

    public async Task<SendMessageResult> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        // Verify conversation exists and is active
        var conversation = await _chatRepository.GetConversationAsync(request.ConversationId, cancellationToken)
            ?? throw new InvalidOperationException("CONVERSATION_NOT_FOUND");

        if (!conversation.CanSendMessages())
            throw new InvalidOperationException("CONVERSATION_UNMATCHED");

        // Verify sender is a participant
        var isParticipant = await _chatRepository.IsParticipantAsync(request.ConversationId, request.SenderId, cancellationToken);
        if (!isParticipant)
            throw new UnauthorizedAccessException("NOT_PARTICIPANT");

        // Persist the message
        var message = new Message(request.ConversationId, request.SenderId, request.Content);
        _chatRepository.AddMessage(message);

        await _chatRepository.SaveChangesAsync(cancellationToken);

        // Publish domain event for notifications
        var match = conversation.Match;
        var recipientId = match.UserId1 == request.SenderId ? match.UserId2 : match.UserId1;
        var contentPreview = request.Content.Length > 100 ? request.Content[..97] + "..." : request.Content;

        await _mediator.Publish(new MessageSentEvent(
            message.Id,
            request.ConversationId,
            request.SenderId,
            recipientId,
            contentPreview), cancellationToken);

        return new SendMessageResult(
            message.Id,
            message.ConversationId,
            message.SenderId,
            message.Content,
            message.SentAt);
    }
}
