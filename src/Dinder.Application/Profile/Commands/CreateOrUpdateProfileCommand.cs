using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Domain.Interfaces;
using MediatR;

namespace Dinder.Application.Profile.Commands;

public sealed record CreateOrUpdateProfileCommand(
    Guid UserId,
    string DisplayName,
    Gender Gender,
    string? Bio) : IRequest<ProfileResult>;

public sealed record ProfileResult(
    Guid Id,
    Guid UserId,
    string DisplayName,
    string? Bio,
    string Gender,
    DateOnly Birthday,
    bool IsDiscoverable,
    double? Latitude,
    double? Longitude,
    int PhotoCount);

public sealed class CreateOrUpdateProfileCommandHandler : IRequestHandler<CreateOrUpdateProfileCommand, ProfileResult>
{
    private readonly IProfileRepository _profileRepository;

    public CreateOrUpdateProfileCommandHandler(IProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    public async Task<ProfileResult> Handle(CreateOrUpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        if (profile is null)
        {
            // Create-on-first-write: profile doesn't exist yet
            // This should only happen if profile wasn't created at registration
            throw new InvalidOperationException("Profile not found. Please complete registration first.");
        }

        profile.Update(request.DisplayName, request.Gender, request.Bio);

        await _profileRepository.SaveChangesAsync(cancellationToken);

        return MapResult(profile);
    }

    private static ProfileResult MapResult(Domain.Entities.Profile profile)
    {
        return new ProfileResult(
            profile.Id,
            profile.UserId,
            profile.DisplayName,
            profile.Bio,
            profile.Gender.ToString(),
            profile.Birthday,
            profile.IsDiscoverable,
            profile.Location?.Y, // latitude
            profile.Location?.X, // longitude
            profile.Photos.Count);
    }
}
