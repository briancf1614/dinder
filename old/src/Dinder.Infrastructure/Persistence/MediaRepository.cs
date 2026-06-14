using Dinder.Domain.Entities;
using Dinder.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Dinder.Infrastructure.Persistence;

public sealed class MediaRepository : IMediaRepository
{
    private readonly MediaDbContext _context;

    public MediaRepository(MediaDbContext context)
    {
        _context = context;
    }

    public async Task<MediaFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.MediaFiles
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<MediaFile?> GetByBlobKeyAsync(string blobKey, CancellationToken cancellationToken = default)
    {
        return await _context.MediaFiles
            .FirstOrDefaultAsync(m => m.BlobKey == blobKey, cancellationToken);
    }

    public async Task<List<MediaFile>> GetByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await _context.MediaFiles
            .Where(m => m.OwnerId == ownerId)
            .ToListAsync(cancellationToken);
    }

    public void Add(MediaFile mediaFile) => _context.MediaFiles.Add(mediaFile);

    public void Update(MediaFile mediaFile) => _context.MediaFiles.Update(mediaFile);

    public void Remove(MediaFile mediaFile) => _context.MediaFiles.Remove(mediaFile);

    // ── GDPR Cascade ────────────────────────────────────────────────────

    public async Task DeleteAllByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        var files = await _context.MediaFiles
            .Where(m => m.OwnerId == ownerId)
            .ToListAsync(cancellationToken);

        _context.MediaFiles.RemoveRange(files);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
