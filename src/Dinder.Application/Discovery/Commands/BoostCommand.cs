using Dinder.Application.Common.Attributes;
using Dinder.Domain.Enums;
using Dinder.Domain.Interfaces;
using MediatR;

namespace Dinder.Application.Discovery.Commands;

[RequiresTier(SubscriptionTier.Premium)]
public sealed record BoostCommand(Guid UserId) : IRequest<BoostResult>;

public sealed record BoostResult(bool Success, string? Message, DateTime? BoostedAt);

public sealed class BoostCommandHandler : IRequestHandler<BoostCommand, BoostResult>
{
    private readonly IProfileRepository _profileRepository;

    public BoostCommandHandler(IProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    public async Task<BoostResult> Handle(BoostCommand request, CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        if (profile is null)
            return new BoostResult(false, "Profile not found.", null);

        var boosted = profile.Boost();

        if (!boosted)
            return new BoostResult(false,
                "You have already boosted your profile this month. " +
                $"Next boost available on {new DateTime(profile.BoostedAt!.Value.AddMonths(1).Year, profile.BoostedAt.Value.AddMonths(1).Month, 1):yyyy-MM-dd}.",
                profile.BoostedAt);

        _profileRepository.Update(profile);
        await _profileRepository.SaveChangesAsync(cancellationToken);

        return new BoostResult(true, "Profile boosted! You will appear at the top of discovery results.", profile.BoostedAt);
    }
}
