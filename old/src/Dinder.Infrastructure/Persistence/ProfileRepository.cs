using Dinder.Domain.Entities;
using Dinder.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Dinder.Infrastructure.Persistence;

public sealed class ProfileRepository : IProfileRepository
{
    private readonly ProfileDbContext _context;

    public ProfileRepository(ProfileDbContext context)
    {
        _context = context;
    }

    public async Task<Profile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Profiles
            .Include(p => p.Photos.OrderBy(ph => ph.SortOrder))
            .Include(p => p.Preference)
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }

    public async Task<Profile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Profiles
            .Include(p => p.Photos.OrderBy(ph => ph.SortOrder))
            .Include(p => p.Preference)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Profiles.AnyAsync(p => p.UserId == userId, cancellationToken);
    }

    public void Add(Profile profile) => _context.Profiles.Add(profile);

    public void Update(Profile profile) => _context.Profiles.Update(profile);

    public void AddPhoto(ProfilePhoto photo) => _context.ProfilePhotos.Add(photo);

    public void UpdatePhoto(ProfilePhoto photo) => _context.ProfilePhotos.Update(photo);

    public void RemovePhoto(ProfilePhoto photo) => _context.ProfilePhotos.Remove(photo);

    public async Task<int> GetPhotoCountAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        return await _context.ProfilePhotos.CountAsync(p => p.ProfileId == profileId, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
