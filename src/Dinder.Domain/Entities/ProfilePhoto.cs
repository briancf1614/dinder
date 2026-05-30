namespace Dinder.Domain.Entities;

public sealed class ProfilePhoto
{
    public Guid Id { get; private set; }
    public Guid ProfileId { get; private set; }
    public Guid? MediaFileId { get; private set; }
    public int SortOrder { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public Profile Profile { get; private set; } = null!;

#pragma warning disable CS8618
    private ProfilePhoto() { } // EF Core
#pragma warning restore CS8618

    public ProfilePhoto(Guid profileId, Guid? mediaFileId, int sortOrder)
    {
        Id = Guid.NewGuid();
        ProfileId = profileId;
        MediaFileId = mediaFileId;
        SortOrder = sortOrder;
        CreatedAt = DateTime.UtcNow;
    }

    public void SetOrder(int order)
    {
        SortOrder = order;
    }
}
