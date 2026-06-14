using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Domain.Events;
using Dinder.Domain.Interfaces;
using MediatR;

namespace Dinder.Application.Profile.Commands;

public sealed record UpdatePreferencesCommand(
    Guid UserId,
    List<Gender> InterestedInGenders,
    int MinAge,
    int MaxAge,
    int MaxDistanceKm) : IRequest<PreferenceResult>;

public sealed record PreferenceResult(
    List<string> InterestedInGenders,
    int MinAge,
    int MaxAge,
    int MaxDistanceKm);

public sealed class UpdatePreferencesCommandHandler : IRequestHandler<UpdatePreferencesCommand, PreferenceResult>
{
    private readonly IProfileRepository _profileRepository;
    private readonly IMediator _mediator;

    public UpdatePreferencesCommandHandler(IProfileRepository profileRepository, IMediator mediator)
    {
        _profileRepository = profileRepository;
        _mediator = mediator;
    }

    public async Task<PreferenceResult> Handle(UpdatePreferencesCommand request, CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetByUserIdAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Profile not found.");

        if (profile.Preference is null)
        {
            var preference = new ProfilePreference(
                profile.Id,
                request.InterestedInGenders,
                request.MinAge,
                request.MaxAge,
                request.MaxDistanceKm);
            profile.SetPreference(preference);
        }
        else
        {
            profile.Preference.Update(
                request.InterestedInGenders,
                request.MinAge,
                request.MaxAge,
                request.MaxDistanceKm);
        }

        profile.UpdateDiscoverability();
        await _profileRepository.SaveChangesAsync(cancellationToken);

        await _mediator.Publish(new ProfileUpdatedEvent(request.UserId, DateTime.UtcNow), cancellationToken);

        return new PreferenceResult(
            request.InterestedInGenders.Select(g => g.ToString()).ToList(),
            request.MinAge,
            request.MaxAge,
            request.MaxDistanceKm);
    }
}
