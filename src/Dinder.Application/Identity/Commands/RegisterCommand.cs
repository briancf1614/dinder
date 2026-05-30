using Dinder.Application.Common.Interfaces;
using Dinder.Domain.Entities;
using Dinder.Domain.Interfaces;
using Dinder.Domain.ValueObjects;
using MediatR;

namespace Dinder.Application.Identity.Commands;

public sealed record RegisterCommand(string Email, string Password) : IRequest<RegisterResult>;

public sealed record RegisterResult(Guid UserId, string AccessToken, string RefreshToken);

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;

    public RegisterCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, IJwtService jwtService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
    }

    public async Task<RegisterResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var email = new Email(request.Email);

        if (await _userRepository.EmailExistsAsync(email, cancellationToken))
            throw new InvalidOperationException("EMAIL_UNAVAILABLE");

        var passwordHash = _passwordHasher.Hash(request.Password);
        var user = new User(email, passwordHash);
        _userRepository.Add(user);

        var (accessToken, refreshToken) = _jwtService.GenerateTokenPair(user.Id, user.Email);
        user.AddRefreshToken(refreshToken, DateTime.UtcNow.AddDays(30));

        await _userRepository.SaveChangesAsync(cancellationToken);

        return new RegisterResult(user.Id, accessToken, refreshToken);
    }
}
