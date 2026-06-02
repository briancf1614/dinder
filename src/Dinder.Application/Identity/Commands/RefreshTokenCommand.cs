using Dinder.Application.Common.Interfaces;
using Dinder.Domain.Entities;
using Dinder.Domain.Interfaces;
using MediatR;

namespace Dinder.Application.Identity.Commands;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<RefreshTokenResult>;

public sealed record RefreshTokenResult(string AccessToken, string RefreshToken);

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;

    public RefreshTokenCommandHandler(IUserRepository userRepository, IJwtService jwtService)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
    }

    public async Task<RefreshTokenResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByRefreshTokenAsync(request.RefreshToken, cancellationToken);
        if (user is null)
            throw new UnauthorizedAccessException("Invalid refresh token.");

        var existingToken = user.RefreshTokens.SingleOrDefault(rt => rt.Token == request.RefreshToken);
        if (existingToken is null)
            throw new UnauthorizedAccessException("Invalid refresh token.");

        // Detect reuse: if the token is already revoked, this is potential theft → revoke all
        if (existingToken.IsRevoked)
        {
            user.RevokeAllRefreshTokens();
            await _userRepository.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedAccessException("Token reuse detected. All sessions revoked.");
        }

        if (existingToken.IsExpired)
        {
            user.RevokeAllRefreshTokens();
            await _userRepository.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedAccessException("Refresh token expired.");
        }

        // Valid rotation: generate new pair, revoke old
        var (newAccessToken, newRefreshToken) = _jwtService.GenerateTokenPair(user.Id, user.Email, tier: user.Tier.ToString());
        existingToken.Revoke(newRefreshToken);
        user.AddRefreshToken(newRefreshToken, DateTime.UtcNow.AddDays(30));

        await _userRepository.SaveChangesAsync(cancellationToken);

        return new RefreshTokenResult(newAccessToken, newRefreshToken);
    }
}
