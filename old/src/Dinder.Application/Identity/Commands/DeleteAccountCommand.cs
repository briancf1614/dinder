using Dinder.Domain.Interfaces;
using MediatR;

namespace Dinder.Application.Identity.Commands;

public sealed record DeleteAccountCommand(Guid UserId) : IRequest;

public sealed class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountCommand>
{
    private readonly IUserRepository _userRepository;

    public DeleteAccountCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            throw new InvalidOperationException("User not found.");

        user.SoftDelete();
        await _userRepository.SaveChangesAsync(cancellationToken);

        // In future phases, publish domain event to cascade deletion across contexts
    }
}
