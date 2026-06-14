using Dinder.Domain.Entities;
using Dinder.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Dinder.Application.Moderation.Commands;

public sealed record BlockUserCommand(Guid BlockerId, Guid BlockedUserId) : IRequest<BlockResult>;

public sealed record BlockResult(Guid BlockId, bool AlreadyBlocked);

public sealed class BlockUserCommandHandler : IRequestHandler<BlockUserCommand, BlockResult>
{
    private readonly IModerationRepository _moderationRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<BlockUserCommandHandler> _logger;

    public BlockUserCommandHandler(
        IModerationRepository moderationRepository,
        IUserRepository userRepository,
        ILogger<BlockUserCommandHandler> logger)
    {
        _moderationRepository = moderationRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<BlockResult> Handle(BlockUserCommand request, CancellationToken cancellationToken)
    {
        // Cannot block yourself
        if (request.BlockerId == request.BlockedUserId)
            throw new InvalidOperationException("You cannot block yourself.");

        // Verify blocked user exists
        var blockedUser = await _userRepository.GetByIdAsync(request.BlockedUserId, cancellationToken);
        if (blockedUser is null)
            throw new InvalidOperationException("User to block not found.");

        // Check if already blocked
        var existingBlock = await _moderationRepository.GetBlockAsync(request.BlockerId, request.BlockedUserId, cancellationToken);
        if (existingBlock is not null)
        {
            _logger.LogDebug("Block already exists: Blocker={BlockerId}, Blocked={BlockedId}",
                request.BlockerId, request.BlockedUserId);
            return new BlockResult(existingBlock.Id, true);
        }

        // Create block — immediate one-way, no notification per SM-2
        var block = new Block(request.BlockerId, request.BlockedUserId);
        _moderationRepository.AddBlock(block);
        await _moderationRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {BlockerId} blocked {BlockedId} (Block: {BlockId})",
            request.BlockerId, request.BlockedUserId, block.Id);

        return new BlockResult(block.Id, false);
    }
}
