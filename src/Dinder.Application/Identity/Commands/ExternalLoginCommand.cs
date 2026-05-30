using Dinder.Application.Common.Interfaces;
using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Domain.Interfaces;
using Dinder.Domain.ValueObjects;
using MediatR;

namespace Dinder.Application.Identity.Commands;

public sealed record ExternalLoginCommand(string Email, ExternalProvider Provider, string ProviderUserId) : IRequest<LoginResult>;

public sealed class ExternalLoginCommandHandler : IRequestHandler<ExternalLoginCommand, LoginResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;

    public ExternalLoginCommandHandler(IUserRepository userRepository, IJwtService jwtService)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
    }

    public async Task<LoginResult> Handle(ExternalLoginCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await _userRepository.GetByExternalLoginAsync(request.Provider, request.ProviderUserId, cancellationToken);

        if (existingUser is not null)
        {
            if (!existingUser.CanAuthenticate())
                throw new UnauthorizedAccessException("Account is not active.");

            var (accessToken, refreshToken) = _jwtService.GenerateTokenPair(existingUser.Id, existingUser.Email);
            existingUser.AddRefreshToken(refreshToken, DateTime.UtcNow.AddDays(30));
            await _userRepository.SaveChangesAsync(cancellationToken);
            return new LoginResult(existingUser.Id, accessToken, refreshToken);
        }

        // Auto-create new user on first social sign-in
        var email = new Email(request.Email);
        var user = User.CreateExternal(email, request.Provider, request.ProviderUserId);
        _userRepository.Add(user);

        var (newAccessToken, newRefreshToken) = _jwtService.GenerateTokenPair(user.Id, user.Email);
        user.AddRefreshToken(newRefreshToken, DateTime.UtcNow.AddDays(30));

        await _userRepository.SaveChangesAsync(cancellationToken);

        return new LoginResult(user.Id, newAccessToken, newRefreshToken);
    }
}
