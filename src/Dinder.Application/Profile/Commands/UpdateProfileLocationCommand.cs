using Dinder.Domain.Interfaces;
using MediatR;

namespace Dinder.Application.Profile.Commands;

public sealed record UpdateProfileLocationCommand(Guid UserId, double Latitude, double Longitude) : IRequest;

public sealed class UpdateProfileLocationCommandHandler : IRequestHandler<UpdateProfileLocationCommand>
{
    private readonly IProfileRepository _profileRepository;

    public UpdateProfileLocationCommandHandler(IProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    public async Task Handle(UpdateProfileLocationCommand request, CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetByUserIdAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Profile not found.");

        profile.SetLocation(request.Latitude, request.Longitude);
        await _profileRepository.SaveChangesAsync(cancellationToken);
    }
}
