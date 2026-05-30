using Dinder.Domain.Interfaces;
using MediatR;

namespace Dinder.Application.Profile.Commands;

public sealed record ReorderPhotosCommand(Guid UserId, List<Guid> PhotoIds) : IRequest;

public sealed class ReorderPhotosCommandHandler : IRequestHandler<ReorderPhotosCommand>
{
    private readonly IProfileRepository _profileRepository;

    public ReorderPhotosCommandHandler(IProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    public async Task Handle(ReorderPhotosCommand request, CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetByUserIdAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Profile not found.");

        profile.ReorderPhotos(request.PhotoIds);
        await _profileRepository.SaveChangesAsync(cancellationToken);
    }
}
