using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dinder.Infrastructure.Persistence.Configurations;

public sealed class PhotoReviewConfiguration : IEntityTypeConfiguration<PhotoReview>
{
    public void Configure(EntityTypeBuilder<PhotoReview> builder)
    {
        builder.ToTable("photo_reviews");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.RejectionReason)
            .HasMaxLength(500);

        builder.Property(x => x.AdultScore);
        builder.Property(x => x.RacyScore);
        builder.Property(x => x.ViolenceScore);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.ReviewedAt);

        // Index for pending review queue
        builder.HasIndex(x => new { x.Status, x.CreatedAt });

        builder.HasIndex(x => x.MediaFileId);
    }
}
