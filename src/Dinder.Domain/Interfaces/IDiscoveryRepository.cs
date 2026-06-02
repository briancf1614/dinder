using Dinder.Domain.Entities;
using Dinder.Domain.Enums;

namespace Dinder.Domain.Interfaces;

public interface IDiscoveryRepository
{
    // Swipes
    Task<Swipe?> GetSwipeAsync(Guid swiperId, Guid swipedId, CancellationToken cancellationToken = default);
    void AddSwipe(Swipe swipe);
    void UpdateSwipe(Swipe swipe);
    void RemoveSwipe(Swipe swipe);
    Task<int> GetDailySwipeCountAsync(Guid swiperId, CancellationToken cancellationToken = default);
    Task<bool> HasSwipedAsync(Guid swiperId, Guid swipedId, CancellationToken cancellationToken = default);

    // Undo
    Task<Swipe?> GetLastSwipeAsync(Guid userId, CancellationToken cancellationToken = default);

    // Likes (who right-swiped the user)
    Task<List<Swipe>> GetLikesForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    // Matches
    Task<Match?> GetMatchAsync(Guid userId1, Guid userId2, CancellationToken cancellationToken = default);
    void AddMatch(Match match);
    Task<List<Match>> GetMatchesForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    // Conversation
    void AddConversation(Conversation conversation);

    // Candidates (raw query)
    Task<List<Profile>> GetCandidatesAsync(
        Guid userId,
        double latitude,
        double longitude,
        int maxDistanceKm,
        List<Gender> interestedInGenders,
        int minAge,
        int maxAge,
        Guid? cursor,
        int limit,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
