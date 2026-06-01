using Dinder.Domain.Entities;
using Dinder.Domain.Enums;

namespace Dinder.Domain.Interfaces;

public interface IAdminRepository
{
    // User search
    Task<List<(User User, DateTime? LastLogin, int ReportCount)>> SearchUsersAsync(
        string query, int skip, int take, CancellationToken cancellationToken = default);
    Task<(User User, DateTime? LastLogin, int ReportCount)?> GetUserDetailsAsync(Guid userId, CancellationToken cancellationToken = default);

    // Audit log (append-only)
    void AddAuditLog(AdminAuditLog entry);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
