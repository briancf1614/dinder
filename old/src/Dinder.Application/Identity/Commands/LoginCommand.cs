using Dinder.Application.Common.Interfaces;
using Dinder.Domain.Events;
using Dinder.Domain.Interfaces;
using MediatR;

namespace Dinder.Application.Identity.Commands;

public sealed record LoginCommand(string Email, string Password) : IRequest<LoginResult>;

public sealed record LoginResult(Guid UserId, string AccessToken, string RefreshToken);

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly IMediator _mediator;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        IMediator mediator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _mediator = mediator;
    }

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null)
            throw new UnauthorizedAccessException("Invalid email or password.");

        if (!user.CanAuthenticate())
            throw new UnauthorizedAccessException("Account is not active.");

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        var (accessToken, refreshToken) = _jwtService.GenerateTokenPair(user.Id, user.Email, tier: user.Tier.ToString());
        user.AddRefreshToken(refreshToken, DateTime.UtcNow.AddDays(30));

        await _userRepository.SaveChangesAsync(cancellationToken);

        // Analytics: track login (fire-and-forget)
        await _mediator.Publish(new UserLoggedInEvent(user.Id, DateTime.UtcNow), cancellationToken);

        return new LoginResult(user.Id, accessToken, refreshToken);
    }
}
