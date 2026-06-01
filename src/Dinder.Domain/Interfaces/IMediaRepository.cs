using Dinder.Domain.Entities;

namespace Dinder.Domain.Interfaces;

public interface IMediaRepository
{
    // Media files
    Task<MediaFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MediaFile?> GetByBlobKeyAsync(string blobKey, CancellationToken cancellationToken = default);
    Task<List<MediaFile>> GetByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default);
    void Add(MediaFile mediaFile);
    void Update(MediaFile mediaFile);
    void Remove(MediaFile mediaFile);

    // GDPR cascade
    Task DeleteAllByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
