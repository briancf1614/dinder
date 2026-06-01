using Dinder.Domain.Interfaces;
using MediatR;

namespace Dinder.Application.Chat.Queries;

public sealed record GetMessagesQuery(Guid ConversationId, Guid UserId, Guid? Cursor = null, int Limit = 50) : IRequest<MessagesResult>;

public sealed record MessagesResult(
    List<MessageDto> Messages,
    Guid? NextCursor);

public sealed record MessageDto(
    Guid MessageId,
    Guid SenderId,
    string Content,
    DateTime SentAt,
    DateTime? ReadAt);

public sealed class GetMessagesQueryHandler : IRequestHandler<GetMessagesQuery, MessagesResult>
{
    private readonly IChatRepository _chatRepository;

    public GetMessagesQueryHandler(IChatRepository chatRepository)
    {
        _chatRepository = chatRepository;
    }

    public async Task<MessagesResult> Handle(GetMessagesQuery request, CancellationToken cancellationToken)
    {
        // Verify participant access
        var isParticipant = await _chatRepository.IsParticipantAsync(request.ConversationId, request.UserId, cancellationToken);
        if (!isParticipant)
            throw new UnauthorizedAccessException("NOT_PARTICIPANT");

        // Verify conversation not unmatched (history not accessible post-unmatch per RC-3)
        var conversation = await _chatRepository.GetConversationAsync(request.ConversationId, cancellationToken);
        if (conversation is not null && !conversation.CanSendMessages())
            throw new UnauthorizedAccessException("CONVERSATION_UNMATCHED");

        // Fetch messages with cursor pagination
        var messages = await _chatRepository.GetMessagesAsync(
            request.ConversationId,
            request.Cursor,
            request.Limit + 1, // Fetch one extra to determine next cursor
            cancellationToken);

        var hasMore = messages.Count > request.Limit;
        if (hasMore)
            messages = messages.Take(request.Limit).ToList();

        var result = messages.Select(m => new MessageDto(
            m.Id,
            m.SenderId,
            m.Content,
            m.SentAt,
            m.ReadAt)).ToList();

        return new MessagesResult(result, hasMore ? messages.Last().Id : null);
    }
}
