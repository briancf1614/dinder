using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Dinder.Application.Moderation.Commands;

public sealed record ReportUserCommand(Guid ReporterId, Guid ReportedUserId, ReportReason Reason, string? Description) : IRequest<ReportResult>;

public sealed record ReportResult(Guid ReportId, bool IsDuplicate);

public sealed class ReportUserCommandHandler : IRequestHandler<ReportUserCommand, ReportResult>
{
    private readonly IModerationRepository _moderationRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ReportUserCommandHandler> _logger;

    public ReportUserCommandHandler(
        IModerationRepository moderationRepository,
        IUserRepository userRepository,
        ILogger<ReportUserCommandHandler> logger)
    {
        _moderationRepository = moderationRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<ReportResult> Handle(ReportUserCommand request, CancellationToken cancellationToken)
    {
        // Cannot report yourself
        if (request.ReporterId == request.ReportedUserId)
            throw new InvalidOperationException("You cannot report yourself.");

        // Verify reported user exists
        var reportedUser = await _userRepository.GetByIdAsync(request.ReportedUserId, cancellationToken);
        if (reportedUser is null)
            throw new InvalidOperationException("Reported user not found.");

        // Dedup check: same reporter + same target
        var alreadyReported = await _moderationRepository.HasReportedAsync(request.ReporterId, request.ReportedUserId, cancellationToken);

        // Still create the report even if duplicate, per SM-1 spec
        var report = new Report(request.ReporterId, request.ReportedUserId, request.Reason, request.Description);
        _moderationRepository.AddReport(report);
        await _moderationRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Report created: {ReportId} by {ReporterId} against {ReportedUserId}, Reason: {Reason}, Duplicate: {IsDuplicate}",
            report.Id, request.ReporterId, request.ReportedUserId, request.Reason, alreadyReported);

        return new ReportResult(report.Id, alreadyReported);
    }
}
