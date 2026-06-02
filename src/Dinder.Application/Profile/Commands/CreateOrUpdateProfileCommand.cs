using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Domain.Events;
using Dinder.Domain.Interfaces;
using MediatR;

namespace Dinder.Application.Profile.Commands;

public sealed record CreateOrUpdateProfileCommand(
    Guid UserId,
    string DisplayName,
    Gender Gender,
    string? Bio,
    List<ProfilePromptDto>? Prompts = null) : IRequest<ProfileResult>;

public sealed record ProfilePromptDto(Guid PromptId, string Answer);

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
    int PhotoCount,
    List<ProfilePromptResultDto>? Prompts);

public sealed class CreateOrUpdateProfileCommandHandler : IRequestHandler<CreateOrUpdateProfileCommand, ProfileResult>
{
    private readonly IProfileRepository _profileRepository;
    private readonly IMediator _mediator;

    public CreateOrUpdateProfileCommandHandler(IProfileRepository profileRepository, IMediator mediator)
    {
        _profileRepository = profileRepository;
        _mediator = mediator;
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

        // Handle prompts if provided
        if (request.Prompts is not null)
        {
            if (request.Prompts.Count > 3)
                throw new InvalidOperationException("PROMPT_LIMIT_EXCEEDED: Maximum 3 prompts allowed.");

            var promptEntities = request.Prompts.Select((p, i) =>
            {
                if (string.IsNullOrWhiteSpace(p.Answer))
                    throw new InvalidOperationException("PROMPT_ANSWER_EMPTY: Answer cannot be empty.");
                if (p.Answer.Length > 150)
                    throw new InvalidOperationException($"PROMPT_ANSWER_TOO_LONG: Answer exceeds 150 characters.");
                return new ProfilePrompt(p.PromptId, p.Answer, i);
            });

            profile.SetPrompts(promptEntities);
        }

        await _profileRepository.SaveChangesAsync(cancellationToken);

        await _mediator.Publish(new ProfileUpdatedEvent(request.UserId, DateTime.UtcNow), cancellationToken);

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
            profile.Photos.Count,
            profile.Prompts.Select(p =>
                new ProfilePromptResultDto(p.PromptId, p.Answer, p.Order)).ToList());
    }
}

public sealed record ProfilePromptResultDto(Guid PromptId, string Answer, int Order);
