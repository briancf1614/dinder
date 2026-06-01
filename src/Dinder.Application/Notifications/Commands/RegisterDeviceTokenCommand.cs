using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Domain.Interfaces;
using MediatR;

namespace Dinder.Application.Notifications.Commands;

public sealed record RegisterDeviceTokenCommand(Guid UserId, string Token, DevicePlatform Platform) : IRequest;

public sealed class RegisterDeviceTokenCommandHandler : IRequestHandler<RegisterDeviceTokenCommand>
{
    private readonly INotificationRepository _notificationRepository;

    public RegisterDeviceTokenCommandHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task Handle(RegisterDeviceTokenCommand request, CancellationToken cancellationToken)
    {
        var existingToken = await _notificationRepository.GetDeviceTokenAsync(request.Token, cancellationToken);

        if (existingToken is not null)
        {
            if (existingToken.UserId != request.UserId)
            {
                // Reassign token to current user (device handover / re-login)
                existingToken.ReassignUser(request.UserId);
                _notificationRepository.UpdateDeviceToken(existingToken);
            }
            // If same user, token is already registered — no-op
        }
        else
        {
            var deviceToken = new DeviceToken(request.UserId, request.Token, request.Platform);
            _notificationRepository.AddDeviceToken(deviceToken);
        }

        await _notificationRepository.SaveChangesAsync(cancellationToken);
    }
}
