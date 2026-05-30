using Dinder.Application.Profile.Commands;
using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Domain.Interfaces;
using MediatR;

namespace Dinder.Application.Profile.Queries;

public sealed record GetProfileQuery(Guid UserId) : IRequest<ProfileResult>;

public sealed class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, ProfileResult>
{
    private readonly IProfileRepository _profileRepository;
    private readonly IUserRepository _userRepository;

    public GetProfileQueryHandler(IProfileRepository profileRepository, IUserRepository userRepository)
    {
        _profileRepository = profileRepository;
        _userRepository = userRepository;
    }

    public async Task<ProfileResult> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        if (profile is null)
        {
            // Create-on-first-read
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
                ?? throw new InvalidOperationException("User not found.");

            profile = new Domain.Entities.Profile(
                request.UserId,
                user.Email.Value.Split('@')[0], // Default display name from email
                Gender.Other,
                user.Birthday ?? DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)));

            _profileRepository.Add(profile);
            await _profileRepository.SaveChangesAsync(cancellationToken);
        }

        return new ProfileResult(
            profile.Id,
            profile.UserId,
            profile.DisplayName,
            profile.Bio,
            profile.Gender.ToString(),
            profile.Birthday,
            profile.IsDiscoverable,
            profile.Location?.Y,
            profile.Location?.X,
            profile.Photos.Count);
    }
}
