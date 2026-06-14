using Dinder.Application.Profile.Commands;
using Dinder.Domain.Interfaces;
using MediatR;

namespace Dinder.Application.Profile.Queries;

public sealed record GetPreferencesQuery(Guid UserId) : IRequest<PreferenceResult?>;

public sealed class GetPreferencesQueryHandler : IRequestHandler<GetPreferencesQuery, PreferenceResult?>
{
    private readonly IProfileRepository _profileRepository;

    public GetPreferencesQueryHandler(IProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    public async Task<PreferenceResult?> Handle(GetPreferencesQuery request, CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile?.Preference is null)
            return null;

        var pref = profile.Preference;
        return new PreferenceResult(
            pref.InterestedInGenders.Select(g => g.ToString()).ToList(),
            pref.MinAge,
            pref.MaxAge,
            pref.MaxDistanceKm);
    }
}
