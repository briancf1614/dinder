using Dinder.Domain.Enums;
using Dinder.Domain.Interfaces;
using MediatR;

namespace Dinder.Application.Discovery.Queries;

public sealed record GetCandidatesQuery(
    Guid UserId,
    double Latitude,
    double Longitude,
    Guid? Cursor = null,
    int Limit = 20) : IRequest<CandidatesResult>;

public sealed record CandidatesResult(
    List<CandidateDto> Candidates,
    Guid? NextCursor);

public sealed record CandidateDto(
    Guid ProfileId,
    Guid UserId,
    string DisplayName,
    string? Bio,
    int Age,
    string Gender,
    double? Latitude,
    double? Longitude,
    int PhotoCount,
    List<CandidatePromptDto>? Prompts);

public sealed record CandidatePromptDto(Guid PromptId, string Answer);

public sealed class GetCandidatesQueryHandler : IRequestHandler<GetCandidatesQuery, CandidatesResult>
{
    private readonly IDiscoveryRepository _discoveryRepository;
    private readonly IProfileRepository _profileRepository;
    private readonly IProfileScorer? _profileScorer;
    private readonly MatchingFeatureFlags _featureFlags;

    public GetCandidatesQueryHandler(
        IDiscoveryRepository discoveryRepository,
        IProfileRepository profileRepository,
        MatchingFeatureFlags featureFlags,
        IProfileScorer? profileScorer = null)
    {
        _discoveryRepository = discoveryRepository;
        _profileRepository = profileRepository;
        _featureFlags = featureFlags;
        _profileScorer = profileScorer;
    }

    public async Task<CandidatesResult> Handle(GetCandidatesQuery request, CancellationToken cancellationToken)
    {
        // Get the current user's profile and preferences
        var profile = await _profileRepository.GetByUserIdAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Profile not found. Please complete your profile first.");

        // Use profile location if available, otherwise use request location
        var latitude = profile.Location?.Y ?? request.Latitude;
        var longitude = profile.Location?.X ?? request.Longitude;

        // Get preferences or use defaults
        var interestedInGenders = profile.Preference?.InterestedInGenders 
            ?? new List<Gender> { Gender.Female, Gender.Male, Gender.NonBinary, Gender.Other };
        var minAge = profile.Preference?.MinAge ?? 18;
        var maxAge = profile.Preference?.MaxAge ?? 100;
        var maxDistanceKm = profile.Preference?.MaxDistanceKm ?? 50;

        var candidates = await _discoveryRepository.GetCandidatesAsync(
            request.UserId,
            latitude,
            longitude,
            maxDistanceKm,
            interestedInGenders,
            minAge,
            maxAge,
            request.Cursor,
            request.Limit + 1, // Fetch one extra to determine if there are more
            cancellationToken);

        var hasMore = candidates.Count > request.Limit;
        if (hasMore)
            candidates = candidates.Take(request.Limit).ToList();

        // ML scoring — re-rank candidates by similarity when feature flag enabled
        if (_featureFlags.UseMLScoring && _profileScorer is not null && candidates.Count > 0)
        {
            var scored = await _profileScorer.ScoreAsync(profile, candidates, cancellationToken);
            // Reorder candidates by score descending while preserving the list
            var scoredMap = scored.ToDictionary(s => s.ProfileId, s => s.Score);
            candidates = candidates
                .OrderByDescending(c => scoredMap.GetValueOrDefault(c.Id, 0))
                .ToList();
        }

        var result = candidates.Select(p => new CandidateDto(
            p.Id,
            p.UserId,
            p.DisplayName,
            p.Bio,
            p.GetAge(),
            p.Gender.ToString(),
            p.Location?.Y,
            p.Location?.X,
            p.Photos.Count,
            p.Prompts.Select(pp => new CandidatePromptDto(pp.PromptId, pp.Answer)).ToList())).ToList();

        return new CandidatesResult(result, hasMore ? candidates.Last().Id : null);
    }
}

/// <summary>Feature flags for ML scoring integration.</summary>
public sealed class MatchingFeatureFlags
{
    public bool UseMLScoring { get; init; }
}
