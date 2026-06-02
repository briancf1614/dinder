using Dinder.Domain.Events;
using Dinder.Domain.Interfaces;
using MediatR;

namespace Dinder.Application.Profile.Commands;

public sealed record UpdateProfileLocationCommand(Guid UserId, double Latitude, double Longitude) : IRequest;

public sealed class UpdateProfileLocationCommandHandler : IRequestHandler<UpdateProfileLocationCommand>
{
    private readonly IProfileRepository _profileRepository;
    private readonly IMediator _mediator;

    public UpdateProfileLocationCommandHandler(IProfileRepository profileRepository, IMediator mediator)
    {
        _profileRepository = profileRepository;
        _mediator = mediator;
    }

    public async Task Handle(UpdateProfileLocationCommand request, CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetByUserIdAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Profile not found.");

        profile.SetLocation(request.Latitude, request.Longitude);
        await _profileRepository.SaveChangesAsync(cancellationToken);

        await _mediator.Publish(new ProfileUpdatedEvent(request.UserId, DateTime.UtcNow), cancellationToken);
    }
}
