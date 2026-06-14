using Dinder.Domain.Entities;

namespace Dinder.Domain.Interfaces;

public interface IProfileRepository
{
    Task<Profile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Profile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default);
    void Add(Profile profile);
    void Update(Profile profile);
    void AddPhoto(ProfilePhoto photo);
    void UpdatePhoto(ProfilePhoto photo);
    void RemovePhoto(ProfilePhoto photo);
    Task<int> GetPhotoCountAsync(Guid profileId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
