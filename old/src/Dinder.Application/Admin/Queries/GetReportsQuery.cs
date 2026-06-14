using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Domain.Interfaces;
using MediatR;

namespace Dinder.Application.Admin.Queries;

public sealed record GetReportsQuery(ReportStatus? Status, string? SubCategory = null) : IRequest<List<ReportSummary>>;

public sealed record ReportSummary(
    Guid Id,
    Guid ReporterId,
    Guid ReportedUserId,
    string Reason,
    string? SubCategory,
    string? Description,
    string Status,
    DateTime CreatedAt,
    string? ResolutionNote,
    DateTime? ResolvedAt);

public sealed class GetReportsQueryHandler : IRequestHandler<GetReportsQuery, List<ReportSummary>>
{
    private readonly IModerationRepository _moderationRepository;

    public GetReportsQueryHandler(IModerationRepository moderationRepository)
    {
        _moderationRepository = moderationRepository;
    }

    public async Task<List<ReportSummary>> Handle(GetReportsQuery request, CancellationToken cancellationToken)
    {
        var reports = await _moderationRepository.GetReportsAsync(request.Status, request.SubCategory, cancellationToken);

        return reports.Select(r => new ReportSummary(
            r.Id,
            r.ReporterId,
            r.ReportedUserId,
            r.Reason.ToString(),
            r.SubCategory,
            r.Description,
            r.Status.ToString(),
            r.CreatedAt,
            r.ResolutionNote,
            r.ResolvedAt
        )).ToList();
    }
}
