using Dinder.Domain.Entities;
using Dinder.Domain.Interfaces;
using MediatR;

namespace Dinder.Application.Chat.Queries;

public sealed record GetConversationsQuery(Guid UserId, Guid? Cursor = null, int Limit = 20)
    : IRequest<ConversationsResult>;

public sealed record ConversationsResult(
    List<ConversationDto> Conversations,
    Guid? NextCursor);

public sealed record ConversationDto(
    Guid ConversationId,
    string DisplayName,
    string? LastMessage,
    int UnreadCount,
    string? IcebreakerQuestion,
    string? IcebreakerCategory);

public sealed class GetConversationsQueryHandler
    : IRequestHandler<GetConversationsQuery, ConversationsResult>
{
    private readonly IChatRepository _chatRepository;
    private readonly IProfileRepository _profileRepository;

    public GetConversationsQueryHandler(
        IChatRepository chatRepository,
        IProfileRepository profileRepository)
    {
        _chatRepository = chatRepository;
        _profileRepository = profileRepository;
    }

    public async Task<ConversationsResult> Handle(
        GetConversationsQuery request, CancellationToken cancellationToken)
    {
        var conversations = await _chatRepository.GetConversationsByUserIdAsync(
            request.UserId, request.Cursor, request.Limit, cancellationToken);

        var hasMore = conversations.Count > request.Limit;
        if (hasMore)
            conversations = conversations.Take(request.Limit).ToList();

        var dtos = new List<ConversationDto>(conversations.Count);
        foreach (var conversation in conversations)
        {
            // Determine the other participant's UserId
            var otherUserId = conversation.Match.UserId1 == request.UserId
                ? conversation.Match.UserId2
                : conversation.Match.UserId1;

            // Look up the match's display name
            var profile = await _profileRepository.GetByUserIdAsync(otherUserId, cancellationToken);
            var displayName = profile?.DisplayName ?? "Unknown";

            // Get unread message count
            var unreadCount = await _chatRepository.GetUnreadMessageCountAsync(
                conversation.Id, request.UserId, cancellationToken);

            // Get the last message preview
            var messages = await _chatRepository.GetMessagesAsync(
                conversation.Id, cursor: null, limit: 1, cancellationToken);
            var lastMessage = messages.FirstOrDefault()?.Content;

            dtos.Add(new ConversationDto(
                conversation.Id,
                displayName,
                lastMessage,
                unreadCount,
                conversation.IcebreakerQuestion,
                conversation.IcebreakerCategory?.ToString()));
        }

        var nextCursor = hasMore && conversations.Count > 0
            ? conversations.Last().Id
            : (Guid?)null;

        return new ConversationsResult(dtos, nextCursor);
    }
}
