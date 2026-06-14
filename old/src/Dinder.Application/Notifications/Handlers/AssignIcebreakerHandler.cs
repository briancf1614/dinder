using Dinder.Domain.Enums;
using Dinder.Domain.Events;
using Dinder.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Dinder.Application.Notifications.Handlers;

/// <summary>
/// When a mutual match is created, assigns a random icebreaker question
/// from the library (weighted by enabled categories) to the conversation.
/// </summary>
public sealed class AssignIcebreakerHandler : INotificationHandler<MatchCreatedEvent>
{
    private readonly IAdminRepository _adminRepository;
    private readonly IChatRepository _chatRepository;
    private readonly ILogger<AssignIcebreakerHandler> _logger;

    public AssignIcebreakerHandler(
        IAdminRepository adminRepository,
        IChatRepository chatRepository,
        ILogger<AssignIcebreakerHandler> logger)
    {
        _adminRepository = adminRepository;
        _chatRepository = chatRepository;
        _logger = logger;
    }

    public async Task Handle(MatchCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            // Get enabled icebreakers from library
            var icebreakers = await _adminRepository.GetEnabledIcebreakerLibraryAsync(cancellationToken);

            if (icebreakers.Count == 0)
            {
                _logger.LogWarning(
                    "No enabled icebreakers in library. Cannot assign for match {MatchId}",
                    notification.MatchId);
                return;
            }

            // Weighted random selection: each enabled category has equal weight.
            // Pick a random category, then a random question within that category.
            var categories = icebreakers
                .Select(i => i.Category)
                .Distinct()
                .ToList();

            var random = Random.Shared;
            var chosenCategory = categories[random.Next(categories.Count)];

            var categoryIcebreakers = icebreakers
                .Where(i => i.Category == chosenCategory)
                .ToList();

            var chosen = categoryIcebreakers[random.Next(categoryIcebreakers.Count)];

            // Find the conversation for this match
            var conversation = await _chatRepository.GetConversationByMatchIdAsync(
                notification.MatchId, cancellationToken);

            if (conversation is null)
            {
                _logger.LogError(
                    "Conversation not found for match {MatchId}. Cannot assign icebreaker.",
                    notification.MatchId);
                return;
            }

            conversation.AssignIcebreaker(chosen.Text, chosen.Category);
            _chatRepository.UpdateConversation(conversation);
            await _chatRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Icebreaker assigned: Match={MatchId}, Category={Category}, Question={Question}",
                notification.MatchId, chosen.Category, chosen.Text);
        }
        catch (Exception ex)
        {
            // Fire-and-forget: failure must not block match creation
            _logger.LogError(ex,
                "Failed to assign icebreaker for match {MatchId}",
                notification.MatchId);
        }
    }
}
