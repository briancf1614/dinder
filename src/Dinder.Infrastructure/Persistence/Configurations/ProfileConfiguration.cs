using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dinder.Infrastructure.Persistence.Configurations;

public sealed class ProfileConfiguration : IEntityTypeConfiguration<Profile>
{
    public void Configure(EntityTypeBuilder<Profile> builder)
    {
        builder.ToTable("profiles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.HasIndex(x => x.UserId)
            .IsUnique();

        builder.Property(x => x.DisplayName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Bio)
            .HasMaxLength(500);

        builder.Property(x => x.Gender)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.Birthday)
            .IsRequired();

        builder.Property(x => x.IsDiscoverable)
            .IsRequired();

        // PostGIS geography(Point, 4326)
        builder.Property(x => x.Location)
            .HasColumnType("geography(Point, 4326)");

        // GiST index for spatial queries
        builder.HasIndex(x => x.Location)
            .HasMethod("GIST");

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        // Index for discovery filtering
        builder.HasIndex(x => x.IsDiscoverable);
        builder.HasIndex(x => x.Gender);
        builder.HasIndex(x => x.Birthday);

        builder.HasOne(x => x.Preference)
            .WithOne(x => x.Profile)
            .HasForeignKey<ProfilePreference>(x => x.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Photos)
            .WithOne(x => x.Profile)
            .HasForeignKey(x => x.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        // Prompts stored as JSONB owned entity (max 3)
        builder.OwnsMany(x => x.Prompts, prompts =>
        {
            prompts.ToJson("prompts");
            prompts.Property(p => p.PromptId).IsRequired();
            prompts.Property(p => p.Answer).HasMaxLength(150).IsRequired();
            prompts.Property(p => p.Order).IsRequired();
        });
    }
}
