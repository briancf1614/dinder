using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dinder.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email)
            .HasMaxLength(256)
            .HasConversion(
                v => v.Value,
                v => new Dinder.Domain.ValueObjects.Email(v))
            .IsRequired();

        builder.HasIndex(x => x.Email)
            .IsUnique()
            .HasFilter("status != 3"); // Exclude soft-deleted from unique constraint

        builder.Property(x => x.PasswordHash)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.BanReason)
            .HasMaxLength(500);

        builder.Property(x => x.Birthday);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.SoftDeletedAt);

        builder.Property(x => x.DailyStreak)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.LastStreakDate);

        builder.Property(x => x.DailyBonusSwipes)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.ProfileCompletenessScore)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.Achievements)
            .HasMaxLength(4000);

        builder.Property(x => x.Tier)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired()
            .HasDefaultValue(SubscriptionTier.Free);

        builder.Property(x => x.StripeCustomerId)
            .HasMaxLength(128);

        builder.HasMany(x => x.ExternalLogins)
            .WithOne()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.RefreshTokens)
            .WithOne()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for login lookup
        builder.HasIndex(x => x.Status);
    }
}
