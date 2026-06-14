using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dinder.Infrastructure.Persistence.Configurations;

public sealed class MediaFileConfiguration : IEntityTypeConfiguration<MediaFile>
{
    public void Configure(EntityTypeBuilder<MediaFile> builder)
    {
        builder.ToTable("media_files");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.BlobKey)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.ContentType)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.FileSizeBytes)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.ApprovedAt);

        // Unique index on blob key
        builder.HasIndex(x => x.BlobKey)
            .IsUnique();

        // Index for owner lookup (GDPR cascade)
        builder.HasIndex(x => x.OwnerId);

        // Index for approved media queries
        builder.HasIndex(x => new { x.Status, x.CreatedAt });
    }
}
