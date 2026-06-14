using Dinder.Domain.Enums;

namespace Dinder.Domain.Entities;

public sealed class ProfilePreference
{
    public Guid Id { get; private set; }
    public Guid ProfileId { get; private set; }
    public List<Gender> InterestedInGenders { get; private set; }
    public int MinAge { get; private set; }
    public int MaxAge { get; private set; }
    public int MaxDistanceKm { get; private set; }

    // Navigation
    public Profile Profile { get; private set; } = null!;

#pragma warning disable CS8618
    private ProfilePreference() { } // EF Core
#pragma warning restore CS8618

    public ProfilePreference(Guid profileId, List<Gender> interestedInGenders, int minAge, int maxAge, int maxDistanceKm)
    {
        Id = Guid.NewGuid();
        ProfileId = profileId;
        InterestedInGenders = interestedInGenders;
        MinAge = minAge;
        MaxAge = maxAge;
        MaxDistanceKm = maxDistanceKm;
    }

    public void Update(List<Gender> interestedInGenders, int minAge, int maxAge, int maxDistanceKm)
    {
        InterestedInGenders = interestedInGenders;
        MinAge = minAge;
        MaxAge = maxAge;
        MaxDistanceKm = maxDistanceKm;
    }
}
