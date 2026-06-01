using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Dinder.Application.Moderation.Commands;

public sealed record UnbanUserCommand(Guid AdminId, Guid TargetUserId, string Reason) : IRequest;

public sealed class UnbanUserCommandHandler : IRequestHandler<UnbanUserCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IAdminRepository _adminRepository;
    private readonly ILogger<UnbanUserCommandHandler> _logger;

    public UnbanUserCommandHandler(
        IUserRepository userRepository,
        IAdminRepository adminRepository,
        ILogger<UnbanUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _adminRepository = adminRepository;
        _logger = logger;
    }

    public async Task Handle(UnbanUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.TargetUserId, cancellationToken);
        if (user is null)
            throw new InvalidOperationException("User not found.");

        if (user.Status != AccountStatus.Banned)
            throw new InvalidOperationException("User is not banned.");

        user.Unban();
        await _userRepository.SaveChangesAsync(cancellationToken);

        // Append-only audit log
        var auditEntry = new AdminAuditLog(request.AdminId, AdminActionType.UnbanUser, request.TargetUserId, request.Reason);
        _adminRepository.AddAuditLog(auditEntry);
        await _adminRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Admin {AdminId} unbanned user {UserId}: {Reason}", request.AdminId, request.TargetUserId, request.Reason);
    }
}
