using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dinder.Infrastructure.Persistence.Configurations;

public sealed class ProfilePreferenceConfiguration : IEntityTypeConfiguration<ProfilePreference>
{
    public void Configure(EntityTypeBuilder<ProfilePreference> builder)
    {
        builder.ToTable("profile_preferences");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProfileId)
            .IsRequired();

        builder.HasIndex(x => x.ProfileId)
            .IsUnique();

        // Store interested-in genders as a comma-separated string
        builder.Property(x => x.InterestedInGenders)
            .HasConversion(
                v => string.Join(",", v.Select(g => g.ToString())),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                      .Select(s => Enum.Parse<Gender>(s))
                      .ToList())
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.MinAge)
            .IsRequired();

        builder.Property(x => x.MaxAge)
            .IsRequired();

        builder.Property(x => x.MaxDistanceKm)
            .IsRequired();
    }
}
