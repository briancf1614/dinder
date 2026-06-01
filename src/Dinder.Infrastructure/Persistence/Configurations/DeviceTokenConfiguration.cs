using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dinder.Infrastructure.Persistence.Configurations;

public sealed class DeviceTokenConfiguration : IEntityTypeConfiguration<DeviceToken>
{
    public void Configure(EntityTypeBuilder<DeviceToken> builder)
    {
        builder.ToTable("device_tokens");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.Token)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.Platform)
            .HasConversion<string>()
            .HasMaxLength(8)
            .IsRequired();

        builder.Property(x => x.IsExpired)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        // Unique index on token for upsert
        builder.HasIndex(x => x.Token)
            .IsUnique();

        // Index for looking up active tokens per user
        builder.HasIndex(x => new { x.UserId, x.IsExpired });
    }
}
