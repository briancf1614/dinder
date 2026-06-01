using Dinder.Domain.Entities;
using Dinder.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Dinder.Infrastructure.Persistence;

public sealed class AdminRepository : IAdminRepository
{
    private readonly DinderDbContext _identityContext;
    private readonly AdminDbContext _adminContext;

    public AdminRepository(DinderDbContext identityContext, AdminDbContext adminContext)
    {
        _identityContext = identityContext;
        _adminContext = adminContext;
    }

    // ── User Search ─────────────────────────────────────────────────────

    public async Task<List<(User User, DateTime? LastLogin, int ReportCount)>> SearchUsersAsync(
        string query, int skip, int take, CancellationToken cancellationToken = default)
    {
        var lowerQuery = query.ToLowerInvariant();

        var users = await _identityContext.Users
            .Where(u => u.Email.Value.Contains(lowerQuery) || u.Id.ToString() == query)
            .OrderBy(u => u.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        var results = new List<(User, DateTime?, int)>();
        foreach (var user in users)
        {
            // Last login from refresh token activity
            var lastToken = await _identityContext.RefreshTokens
                .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
                .OrderByDescending(rt => rt.CreatedAt)
                .Select(rt => (DateTime?)rt.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            // Report count: cross-context query via admin's own schema
            // Since reports live in moderation schema, we check via moderation context
            // For now, return 0 (full cross-context needs ModerationDbContext injection)
            int reportCount = 0;

            results.Add((user, lastToken, reportCount));
        }

        return results;
    }

    public async Task<(User User, DateTime? LastLogin, int ReportCount)?> GetUserDetailsAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _identityContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return null;

        var lastToken = await _identityContext.RefreshTokens
            .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
            .OrderByDescending(rt => rt.CreatedAt)
            .Select(rt => (DateTime?)rt.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return (user, lastToken, 0);
    }

    // ── Audit Log ───────────────────────────────────────────────────────

    public void AddAuditLog(AdminAuditLog entry) => _adminContext.AuditLogs.Add(entry);

    // ── Save ────────────────────────────────────────────────────────────

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _identityContext.SaveChangesAsync(cancellationToken);
        await _adminContext.SaveChangesAsync(cancellationToken);
    }
}
