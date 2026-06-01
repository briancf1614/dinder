using Dinder.Domain.Interfaces;
using MediatR;

namespace Dinder.Application.Admin.Queries;

public sealed record AdminGetUsersQuery(string Query, int Page = 1, int PageSize = 50) : IRequest<AdminUsersResult>;

public sealed record AdminUsersResult(
    List<AdminUserSummary> Users,
    int Total,
    int Page,
    int PageSize);

public sealed class AdminGetUsersQueryHandler : IRequestHandler<AdminGetUsersQuery, AdminUsersResult>
{
    private readonly IAdminRepository _adminRepository;

    public AdminGetUsersQueryHandler(IAdminRepository adminRepository)
    {
        _adminRepository = adminRepository;
    }

    public async Task<AdminUsersResult> Handle(AdminGetUsersQuery request, CancellationToken cancellationToken)
    {
        var skip = (request.Page - 1) * request.PageSize;
        var users = await _adminRepository.SearchUsersAsync(request.Query, skip, request.PageSize, cancellationToken);

        // We approximate total (EF doesn't give us a clean count across the search easily)
        var summaries = users.Select(u => new AdminUserSummary(
            u.User.Id,
            u.User.Email.Value,
            u.User.Status.ToString(),
            u.User.CreatedAt,
            u.LastLogin,
            u.ReportCount,
            u.User.BanReason
        )).ToList();

        return new AdminUsersResult(summaries, summaries.Count, request.Page, request.PageSize);
    }
}

public sealed record AdminUserSummary(
    Guid UserId,
    string Email,
    string Status,
    DateTime CreatedAt,
    DateTime? LastLogin,
    int ReportCount,
    string? BanReason);
