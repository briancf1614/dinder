using Dinder.Domain.Entities;
using Dinder.Domain.Interfaces;
using MediatR;

namespace Dinder.Application.Profile.Commands;

public sealed record PromptItem(Guid PromptId, string Answer);

public sealed record UpdateProfilePromptsCommand(
    Guid UserId,
    List<PromptItem> Prompts) : IRequest;

public sealed record PromptCatalogDto(
    Guid Id,
    string Text,
    string Category,
    bool IsEnabled);

public sealed class UpdateProfilePromptsCommandHandler : IRequestHandler<UpdateProfilePromptsCommand>
{
    private readonly IProfileRepository _profileRepository;
    private readonly IAdminRepository _adminRepository;

    public UpdateProfilePromptsCommandHandler(
        IProfileRepository profileRepository,
        IAdminRepository adminRepository)
    {
        _profileRepository = profileRepository;
        _adminRepository = adminRepository;
    }

    public async Task Handle(UpdateProfilePromptsCommand request, CancellationToken cancellationToken)
    {
        // Validate max 3 prompts
        if (request.Prompts.Count > 3)
            throw new InvalidOperationException("PROMPT_LIMIT_EXCEEDED: Maximum 3 prompts allowed.");

        // Validate each answer ≤ 150 chars
        foreach (var item in request.Prompts)
        {
            if (string.IsNullOrWhiteSpace(item.Answer))
                throw new InvalidOperationException("PROMPT_ANSWER_EMPTY: Answer cannot be empty.");

            if (item.Answer.Length > 150)
                throw new InvalidOperationException($"PROMPT_ANSWER_TOO_LONG: Answer exceeds 150 characters (was {item.Answer.Length}).");
        }

        // Validate all prompt IDs exist in catalog
        var catalogPrompts = await _adminRepository.GetEnabledPromptCatalogAsync(cancellationToken);
        var validIds = new HashSet<Guid>(catalogPrompts.Select(p => p.Id));
        foreach (var item in request.Prompts)
        {
            if (!validIds.Contains(item.PromptId))
                throw new InvalidOperationException($"PROMPT_NOT_FOUND: Prompt '{item.PromptId}' is not in the enabled catalog.");
        }

        var profile = await _profileRepository.GetByUserIdAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException("PROFILE_NOT_FOUND");

        var promptEntities = request.Prompts.Select((p, i) =>
            new ProfilePrompt(p.PromptId, p.Answer, i));

        profile.SetPrompts(promptEntities);

        await _profileRepository.SaveChangesAsync(cancellationToken);
    }
}

public sealed class GetPromptCatalogQuery : IRequest<List<PromptCatalogDto>>;

public sealed class GetPromptCatalogQueryHandler : IRequestHandler<GetPromptCatalogQuery, List<PromptCatalogDto>>
{
    private readonly IAdminRepository _adminRepository;

    public GetPromptCatalogQueryHandler(IAdminRepository adminRepository)
    {
        _adminRepository = adminRepository;
    }

    public async Task<List<PromptCatalogDto>> Handle(GetPromptCatalogQuery request, CancellationToken cancellationToken)
    {
        var prompts = await _adminRepository.GetEnabledPromptCatalogAsync(cancellationToken);

        return prompts.Select(p => new PromptCatalogDto(
            p.Id,
            p.Text,
            p.Category.ToString(),
            p.IsEnabled)).ToList();
    }
}
