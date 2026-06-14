using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Domain.Interfaces;
using MediatR;

namespace Dinder.Application.Admin.Commands;

// ── Create ─────────────────────────────────────────────────────────────

public sealed record CreatePromptCommand(string Text, PromptCategory Category) : IRequest<Guid>;

public sealed class CreatePromptCommandHandler : IRequestHandler<CreatePromptCommand, Guid>
{
    private readonly IAdminRepository _adminRepository;

    public CreatePromptCommandHandler(IAdminRepository adminRepository)
    {
        _adminRepository = adminRepository;
    }

    public async Task<Guid> Handle(CreatePromptCommand request, CancellationToken cancellationToken)
    {
        var prompt = new PromptCatalog(request.Text, request.Category);
        _adminRepository.AddPrompt(prompt);
        await _adminRepository.SaveChangesAsync(cancellationToken);
        return prompt.Id;
    }
}

// ── Update ─────────────────────────────────────────────────────────────

public sealed record UpdatePromptCommand(Guid Id, string Text, PromptCategory Category, bool IsEnabled) : IRequest;

public sealed class UpdatePromptCommandHandler : IRequestHandler<UpdatePromptCommand>
{
    private readonly IAdminRepository _adminRepository;

    public UpdatePromptCommandHandler(IAdminRepository adminRepository)
    {
        _adminRepository = adminRepository;
    }

    public async Task Handle(UpdatePromptCommand request, CancellationToken cancellationToken)
    {
        var prompt = await _adminRepository.GetPromptCatalogByIdAsync(request.Id, cancellationToken)
            ?? throw new InvalidOperationException("PROMPT_NOT_FOUND");

        prompt.Update(request.Text, request.Category);

        if (request.IsEnabled)
            prompt.Enable();
        else
            prompt.Disable();

        _adminRepository.UpdatePrompt(prompt);
        await _adminRepository.SaveChangesAsync(cancellationToken);
    }
}
