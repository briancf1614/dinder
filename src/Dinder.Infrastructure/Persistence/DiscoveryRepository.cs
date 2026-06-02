using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace Dinder.Infrastructure.Persistence;

public sealed class DiscoveryRepository : IDiscoveryRepository
{
    private readonly DiscoveryDbContext _discoveryContext;
    private readonly ProfileDbContext _profileContext;

    public DiscoveryRepository(DiscoveryDbContext discoveryContext, ProfileDbContext profileContext)
    {
        _discoveryContext = discoveryContext;
        _profileContext = profileContext;
    }

    // ── Swipes ──────────────────────────────────────────────────────────

    public async Task<Swipe?> GetSwipeAsync(Guid swiperId, Guid swipedId, CancellationToken cancellationToken = default)
    {
        return await _discoveryContext.Swipes
            .FirstOrDefaultAsync(s => s.SwiperId == swiperId && s.SwipedId == swipedId, cancellationToken);
    }

    public void AddSwipe(Swipe swipe) => _discoveryContext.Swipes.Add(swipe);

    public void UpdateSwipe(Swipe swipe) => _discoveryContext.Swipes.Update(swipe);

    public void RemoveSwipe(Swipe swipe) => _discoveryContext.Swipes.Remove(swipe);

    public async Task<int> GetDailySwipeCountAsync(Guid swiperId, CancellationToken cancellationToken = default)
    {
        var todayUtc = DateTime.UtcNow.Date;
        return await _discoveryContext.Swipes
            .CountAsync(s => s.SwiperId == swiperId && s.CreatedAt >= todayUtc, cancellationToken);
    }

    public async Task<bool> HasSwipedAsync(Guid swiperId, Guid swipedId, CancellationToken cancellationToken = default)
    {
        return await _discoveryContext.Swipes
            .AnyAsync(s => s.SwiperId == swiperId && s.SwipedId == swipedId, cancellationToken);
    }

    // ── Undo ──────────────────────────────────────────────────────────

    public async Task<Swipe?> GetLastSwipeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _discoveryContext.Swipes
            .Where(s => s.SwiperId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    // ── Likes ─────────────────────────────────────────────────────────

    public async Task<List<Swipe>> GetLikesForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Users who right-swiped the current user, excluding those the user has already swiped
        var alreadySwipedIds = await _discoveryContext.Swipes
            .Where(s => s.SwiperId == userId)
            .Select(s => s.SwipedId)
            .ToListAsync(cancellationToken);

        return await _discoveryContext.Swipes
            .Where(s => s.SwipedId == userId
                        && s.Direction == SwipeDirection.Right
                        && !alreadySwipedIds.Contains(s.SwiperId))
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    // ── Matches ─────────────────────────────────────────────────────────

    public async Task<Match?> GetMatchAsync(Guid userId1, Guid userId2, CancellationToken cancellationToken = default)
    {
        // Check both orderings
        return await _discoveryContext.Matches
            .FirstOrDefaultAsync(m =>
                (m.UserId1 == userId1 && m.UserId2 == userId2) ||
                (m.UserId1 == userId2 && m.UserId2 == userId1),
                cancellationToken);
    }

    public void AddMatch(Match match) => _discoveryContext.Matches.Add(match);

    public async Task<List<Match>> GetMatchesForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _discoveryContext.Matches
            .Where(m => m.UserId1 == userId || m.UserId2 == userId)
            .Include(m => m.Conversation)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    // ── Conversation ────────────────────────────────────────────────────

    public void AddConversation(Conversation conversation) => _discoveryContext.Conversations.Add(conversation);

    // ── Candidates ──────────────────────────────────────────────────────

    public async Task<List<Profile>> GetCandidatesAsync(
        Guid userId,
        double latitude,
        double longitude,
        int maxDistanceKm,
        List<Gender> interestedInGenders,
        int minAge,
        int maxAge,
        Guid? cursor,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var minBirthday = today.AddYears(-maxAge);
        var maxBirthday = today.AddYears(-minAge);

        // Get IDs of already-swiped users
        var swipedUserIds = await _discoveryContext.Swipes
            .Where(s => s.SwiperId == userId)
            .Select(s => s.SwipedId)
            .ToListAsync(cancellationToken);

        var userLocation = new Point(longitude, latitude) { SRID = 4326 };

        var query = _profileContext.Profiles
            .Include(p => p.Photos.OrderBy(ph => ph.SortOrder))
            .Where(p => p.IsDiscoverable)
            .Where(p => p.UserId != userId) // Exclude self
            .Where(p => !swipedUserIds.Contains(p.UserId)) // Exclude already swiped
            .Where(p => interestedInGenders.Contains(p.Gender)) // Gender filter
            .Where(p => p.Birthday >= minBirthday && p.Birthday <= maxBirthday); // Age filter

        // Spatial proximity filter using ST_DWithin (via EF.Functions)
        // Only include profiles that have a location set
        query = query.Where(p =>
            p.Location != null &&
            p.Location.IsWithinDistance(userLocation, maxDistanceKm * 1000)); // Convert km to meters

        // Cursor-based pagination (ordered by last active recency)
        if (cursor.HasValue)
        {
            query = query.Where(p => p.Id.CompareTo(cursor.Value) > 0);
        }

        query = query.OrderByDescending(p => p.UpdatedAt)
                     .ThenBy(p => p.Id);

        return await query.Take(limit).ToListAsync(cancellationToken);
    }

    // ── Save ────────────────────────────────────────────────────────────

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _discoveryContext.SaveChangesAsync(cancellationToken);
    }
}
