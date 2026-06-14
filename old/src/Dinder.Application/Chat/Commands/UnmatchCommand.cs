using Dinder.Domain.Interfaces;
using MediatR;

namespace Dinder.Application.Chat.Commands;

public sealed record UnmatchCommand(Guid ConversationId, Guid UserId) : IRequest;

public sealed class UnmatchCommandHandler : IRequestHandler<UnmatchCommand>
{
    private readonly IChatRepository _chatRepository;

    public UnmatchCommandHandler(IChatRepository chatRepository)
    {
        _chatRepository = chatRepository;
    }

    public async Task Handle(UnmatchCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _chatRepository.GetConversationAsync(request.ConversationId, cancellationToken)
            ?? throw new InvalidOperationException("CONVERSATION_NOT_FOUND");

        if (!conversation.IsParticipant(request.UserId, conversation.Match.UserId1, conversation.Match.UserId2))
            throw new UnauthorizedAccessException("NOT_PARTICIPANT");

        if (!conversation.CanSendMessages())
            throw new InvalidOperationException("ALREADY_UNMATCHED");

        conversation.Unmatch(request.UserId);

        await _chatRepository.SaveChangesAsync(cancellationToken);
    }
}
