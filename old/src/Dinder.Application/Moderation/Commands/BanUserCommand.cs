using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Domain.Events;
using Dinder.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Dinder.Application.Moderation.Commands;

public sealed record BanUserCommand(Guid AdminId, Guid TargetUserId, string Reason) : IRequest;

public sealed class BanUserCommandHandler : IRequestHandler<BanUserCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IAdminRepository _adminRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<BanUserCommandHandler> _logger;

    public BanUserCommandHandler(
        IUserRepository userRepository,
        IAdminRepository adminRepository,
        IMediator mediator,
        ILogger<BanUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _adminRepository = adminRepository;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(BanUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.TargetUserId, cancellationToken);
        if (user is null)
            throw new InvalidOperationException("User not found.");

        if (user.Status == AccountStatus.Banned)
            throw new InvalidOperationException("User is already banned.");

        user.Ban(request.Reason);
        await _userRepository.SaveChangesAsync(cancellationToken);

        // Append-only audit log
        var auditEntry = new AdminAuditLog(request.AdminId, AdminActionType.BanUser, request.TargetUserId, request.Reason);
        _adminRepository.AddAuditLog(auditEntry);
        await _adminRepository.SaveChangesAsync(cancellationToken);

        // Publish domain event — SignalR hubs will respond to terminate connections
        await _mediator.Publish(new UserBannedEvent(request.TargetUserId, request.AdminId, request.Reason), cancellationToken);

        _logger.LogWarning("Admin {AdminId} banned user {UserId}: {Reason}", request.AdminId, request.TargetUserId, request.Reason);
    }
}
