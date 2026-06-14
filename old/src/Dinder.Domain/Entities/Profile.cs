using Dinder.Domain.Enums;
using NetTopologySuite.Geometries;

namespace Dinder.Domain.Entities;

public sealed class Profile
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string DisplayName { get; private set; }
    public string? Bio { get; private set; }
    public Gender Gender { get; private set; }
    public DateOnly Birthday { get; private set; }
    public bool IsDiscoverable { get; private set; }
    public Point? Location { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? BoostedAt { get; private set; }

    private readonly List<ProfilePhoto> _photos = [];
    public IReadOnlyCollection<ProfilePhoto> Photos => _photos.AsReadOnly();

    private readonly List<ProfilePrompt> _prompts = [];
    public IReadOnlyCollection<ProfilePrompt> Prompts => _prompts.AsReadOnly();

    public ProfilePreference? Preference { get; private set; }

#pragma warning disable CS8618
    private Profile() { } // EF Core
#pragma warning restore CS8618

    public Profile(Guid userId, string displayName, Gender gender, DateOnly birthday)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        DisplayName = displayName;
        Gender = gender;
        Birthday = birthday;
        IsDiscoverable = false;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(string displayName, Gender gender, string? bio)
    {
        DisplayName = displayName;
        Gender = gender;
        Bio = bio;
        UpdatedAt = DateTime.UtcNow;
        UpdateDiscoverability();
    }

    public void SetLocation(double latitude, double longitude)
    {
        // SRID 4326 = WGS84
        Location = new Point(longitude, latitude) { SRID = 4326 };
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddPhoto(ProfilePhoto photo)
    {
        _photos.Add(photo);
        UpdateDiscoverability();
    }

    public void RemovePhoto(Guid photoId)
    {
        _photos.RemoveAll(p => p.Id == photoId);
        UpdateDiscoverability();
    }

    public void ReorderPhotos(List<Guid> photoIds)
    {
        for (int i = 0; i < photoIds.Count; i++)
        {
            var photo = _photos.FirstOrDefault(p => p.Id == photoIds[i]);
            if (photo is not null)
                photo.SetOrder(i);
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPrompts(IEnumerable<ProfilePrompt> prompts)
    {
        _prompts.Clear();
        _prompts.AddRange(prompts);
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReorderPrompts(List<Guid> promptIds)
    {
        for (int i = 0; i < promptIds.Count; i++)
        {
            var prompt = _prompts.FirstOrDefault(p => p.PromptId == promptIds[i]);
            if (prompt is not null)
                prompt.SetOrder(i);
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPreference(ProfilePreference preference)
    {
        Preference = preference;
        UpdateDiscoverability();
    }

    public void UpdateDiscoverability()
    {
        // IsDiscoverable when: bio is set AND preferences exist AND at least one photo exists
        // Photo approval is handled by the moderation system (Phase 6)
        IsDiscoverable = !string.IsNullOrWhiteSpace(Bio) 
                         && Preference is not null 
                         && _photos.Count > 0;
    }

    /// <summary>
    /// Bumps the profile to the top of candidate results and records the boost timestamp.
    /// Returns false if the profile was already boosted this calendar month.
    /// </summary>
    public bool Boost()
    {
        var now = DateTime.UtcNow;
        if (BoostedAt.HasValue
            && BoostedAt.Value.Year == now.Year
            && BoostedAt.Value.Month == now.Month)
        {
            return false;
        }

        BoostedAt = now;
        UpdatedAt = now;
        return true;
    }

    public int GetAge()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - Birthday.Year;
        if (Birthday > today.AddYears(-age))
            age--;
        return age;
    }
}
