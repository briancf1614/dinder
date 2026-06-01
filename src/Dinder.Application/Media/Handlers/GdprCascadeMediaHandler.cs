using Dinder.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Dinder.Application.Media.Handlers;

/// <summary>
/// Handles GDPR cascade: deletes all user blobs and MediaFile records on account deletion.
/// This is triggered as part of the account deletion workflow (future integration point).
/// </summary>
public sealed class GdprCascadeMediaHandler
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IBlobStorageService _blobStorage;
    private readonly ILogger<GdprCascadeMediaHandler> _logger;

    public GdprCascadeMediaHandler(
        IMediaRepository mediaRepository,
        IBlobStorageService blobStorage,
        ILogger<GdprCascadeMediaHandler> logger)
    {
        _mediaRepository = mediaRepository;
        _blobStorage = blobStorage;
        _logger = logger;
    }

    public async Task CascadeDeleteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GDPR cascade: Deleting all media for user {UserId}", userId);

        try
        {
            // Delete all blobs from storage
            await _blobStorage.DeleteUserBlobsAsync(userId, cancellationToken);
            _logger.LogDebug("Deleted all blobs for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete blobs for user {UserId}", userId);
            throw;
        }

        try
        {
            // Delete all MediaFile records
            await _mediaRepository.DeleteAllByOwnerAsync(userId, cancellationToken);
            await _mediaRepository.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Deleted all MediaFile records for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete MediaFile records for user {UserId}", userId);
            throw;
        }

        _logger.LogInformation("GDPR cascade complete for user {UserId}", userId);
    }
}
