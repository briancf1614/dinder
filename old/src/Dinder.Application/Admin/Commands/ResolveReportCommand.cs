using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Dinder.Application.Admin.Commands;

public sealed record ResolveReportCommand(Guid AdminId, Guid ReportId, string Resolution, string Note) : IRequest;

public sealed class ResolveReportCommandHandler : IRequestHandler<ResolveReportCommand>
{
    private readonly IModerationRepository _moderationRepository;
    private readonly IAdminRepository _adminRepository;
    private readonly ILogger<ResolveReportCommandHandler> _logger;

    public ResolveReportCommandHandler(
        IModerationRepository moderationRepository,
        IAdminRepository adminRepository,
        ILogger<ResolveReportCommandHandler> logger)
    {
        _moderationRepository = moderationRepository;
        _adminRepository = adminRepository;
        _logger = logger;
    }

    public async Task Handle(ResolveReportCommand request, CancellationToken cancellationToken)
    {
        var report = await _moderationRepository.GetReportAsync(request.ReportId, cancellationToken);
        if (report is null)
            throw new InvalidOperationException("Report not found.");

        if (report.Status != ReportStatus.Pending)
            throw new InvalidOperationException("Report is already resolved or dismissed.");

        switch (request.Resolution.ToLowerInvariant())
        {
            case "resolved":
                report.Resolve(request.Note);
                break;
            case "dismissed":
                report.Dismiss(request.Note);
                break;
            default:
                throw new InvalidOperationException("Invalid resolution. Must be 'Resolved' or 'Dismissed'.");
        }

        await _moderationRepository.SaveChangesAsync(cancellationToken);

        // Audit log
        var auditAction = request.Resolution.Equals("Resolved", StringComparison.OrdinalIgnoreCase)
            ? AdminActionType.ResolveReport : AdminActionType.DismissReport;
        var auditEntry = new AdminAuditLog(request.AdminId, auditAction, report.ReportedUserId, request.Note);
        _adminRepository.AddAuditLog(auditEntry);
        await _adminRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Admin {AdminId} {Action} report {ReportId}: {Note}",
            request.AdminId, request.Resolution, request.ReportId, request.Note);
    }
}
