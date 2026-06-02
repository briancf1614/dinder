using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Domain.Interfaces;
using MediatR;

namespace Dinder.Application.Admin.Commands;

// ── Create ─────────────────────────────────────────────────────────────

public sealed record CreateIcebreakerCommand(string Text, IcebreakerCategory Category) : IRequest<Guid>;

public sealed class CreateIcebreakerCommandHandler : IRequestHandler<CreateIcebreakerCommand, Guid>
{
    private readonly IAdminRepository _adminRepository;

    public CreateIcebreakerCommandHandler(IAdminRepository adminRepository)
    {
        _adminRepository = adminRepository;
    }

    public async Task<Guid> Handle(CreateIcebreakerCommand request, CancellationToken cancellationToken)
    {
        var icebreaker = new IcebreakerLibrary(request.Text, request.Category);
        _adminRepository.AddIcebreaker(icebreaker);
        await _adminRepository.SaveChangesAsync(cancellationToken);
        return icebreaker.Id;
    }
}

// ── Update ─────────────────────────────────────────────────────────────

public sealed record UpdateIcebreakerCommand(Guid Id, string Text, IcebreakerCategory Category, bool IsEnabled) : IRequest;

public sealed class UpdateIcebreakerCommandHandler : IRequestHandler<UpdateIcebreakerCommand>
{
    private readonly IAdminRepository _adminRepository;

    public UpdateIcebreakerCommandHandler(IAdminRepository adminRepository)
    {
        _adminRepository = adminRepository;
    }

    public async Task Handle(UpdateIcebreakerCommand request, CancellationToken cancellationToken)
    {
        var icebreaker = await _adminRepository.GetIcebreakerLibraryByIdAsync(request.Id, cancellationToken)
            ?? throw new InvalidOperationException("ICEBREAKER_NOT_FOUND");

        icebreaker.Update(request.Text, request.Category);

        if (request.IsEnabled)
            icebreaker.Enable();
        else
            icebreaker.Disable();

        _adminRepository.UpdateIcebreaker(icebreaker);
        await _adminRepository.SaveChangesAsync(cancellationToken);
    }
}
