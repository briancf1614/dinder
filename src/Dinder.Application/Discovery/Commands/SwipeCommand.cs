using System.Security.Claims;
using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Dinder.Domain.Events;
using Dinder.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Dinder.Application.Discovery.Commands;

public sealed record SwipeCommand(Guid SwiperId, Guid SwipedId, SwipeDirection Direction) : IRequest<SwipeResult>;

public sealed record SwipeResult(bool IsMatch, Guid? MatchId);

public sealed class SwipeCommandHandler : IRequestHandler<SwipeCommand, SwipeResult>
{
    private const int FreeDailyLimit = 25;
    private const int PlusDailyLimit = 100;
    private const int PremiumDailyLimit = int.MaxValue;

    private readonly IDiscoveryRepository _discoveryRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMediator _mediator;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SwipeCommandHandler(
        IDiscoveryRepository discoveryRepository,
        IUserRepository userRepository,
        IMediator mediator,
        IHttpContextAccessor httpContextAccessor)
    {
        _discoveryRepository = discoveryRepository;
        _userRepository = userRepository;
        _mediator = mediator;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<SwipeResult> Handle(SwipeCommand request, CancellationToken cancellationToken)
    {
        // ── Resolve user's tier from JWT ──────────────────────────────
        var tier = ResolveTier();

        // ── Determine daily limit based on tier ───────────────────────
        var baseLimit = tier switch
        {
            SubscriptionTier.Premium => PremiumDailyLimit,
            SubscriptionTier.Plus => PlusDailyLimit,
            _ => FreeDailyLimit
        };

        // ── Add bonus swipes from gamification streak ──────────────────
        var bonusSwipes = await GetBonusSwipesAsync(request.SwiperId, cancellationToken);
        var dailyLimit = baseLimit == int.MaxValue
            ? int.MaxValue // Premium remains unlimited
            : baseLimit + bonusSwipes;

        // ── Check daily swipe limit ───────────────────────────────────
        var dailyCount = await _discoveryRepository.GetDailySwipeCountAsync(
            request.SwiperId, cancellationToken);

        if (dailyCount >= dailyLimit)
        {
            var resetTime = DateTime.UtcNow.Date.AddDays(1);
            var nextTier = tier switch
            {
                SubscriptionTier.Free => "Plus",
                SubscriptionTier.Plus => "Premium",
                _ => null // Premium never hits this path
            };

            if (nextTier is not null)
            {
                throw new InvalidOperationException(
                    $"SWIPE_LIMIT_REACHED:{resetTime:O}:{nextTier}");
            }

            // Fallback (shouldn't happen for Premium but defensive)
            throw new InvalidOperationException(
                $"SWIPE_LIMIT_REACHED:{resetTime:O}");
        }

        // ── Check for existing swipe (idempotent upsert) ─────────────
        var existingSwipe = await _discoveryRepository.GetSwipeAsync(
            request.SwiperId, request.SwipedId, cancellationToken);

        if (existingSwipe is not null)
        {
            // Update direction if different
            existingSwipe.UpdateDirection(request.Direction);
        }
        else
        {
            var swipe = new Swipe(request.SwiperId, request.SwipedId, request.Direction);
            _discoveryRepository.AddSwipe(swipe);
        }

        // ── Check for mutual match if swiping right ───────────────────
        Match? match = null;
        if (request.Direction == SwipeDirection.Right)
        {
            var reverseSwipe = await _discoveryRepository.GetSwipeAsync(
                request.SwipedId, request.SwiperId, cancellationToken);

            if (reverseSwipe?.Direction == SwipeDirection.Right)
            {
                // Mutual match detected — create Match + Conversation atomically
                match = new Match(request.SwiperId, request.SwipedId);
                _discoveryRepository.AddMatch(match);

                var conversation = new Conversation(match.Id);
                _discoveryRepository.AddConversation(conversation);
            }
        }

        await _discoveryRepository.SaveChangesAsync(cancellationToken);

        // ── Publish domain events ─────────────────────────────────────
        // Analytics: track every swipe (fire-and-forget)
        await _mediator.Publish(
            new SwipeRecordedEvent(Guid.NewGuid(), request.SwiperId, request.SwipedId, request.Direction.ToString()),
            cancellationToken);

        if (match is not null)
        {
            await _mediator.Publish(
                new MatchCreatedEvent(match.Id, match.UserId1, match.UserId2),
                cancellationToken);
        }

        return new SwipeResult(match is not null, match?.Id);
    }

    private SubscriptionTier ResolveTier()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
            return SubscriptionTier.Free;

        var user = httpContext.User;
        if (user.Identity?.IsAuthenticated != true)
            return SubscriptionTier.Free;

        var tierClaim = user.FindFirstValue("tier");
        if (string.IsNullOrWhiteSpace(tierClaim)
            || !Enum.TryParse<SubscriptionTier>(tierClaim, out var tier))
        {
            return SubscriptionTier.Free;
        }

        return tier;
    }

    private async Task<int> GetBonusSwipesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null) return 0;

        // Calculate bonus from streak (capped at 30-day milestone = +15)
        return user.DailyStreak switch
        {
            >= 30 => 15,
            >= 14 => 10,
            >= 7 => 5,
            _ => 0
        };
    }
}
