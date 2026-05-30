using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dinder.Infrastructure.Persistence.Configurations;

public sealed class SwipeConfiguration : IEntityTypeConfiguration<Swipe>
{
    public void Configure(EntityTypeBuilder<Swipe> builder)
    {
        builder.ToTable("swipes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SwiperId)
            .IsRequired();

        builder.Property(x => x.SwipedId)
            .IsRequired();

        builder.Property(x => x.Direction)
            .HasConversion<string>()
            .HasMaxLength(8)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // Unique index to support idempotent upsert (one swipe per pair)
        builder.HasIndex(x => new { x.SwiperId, x.SwipedId })
            .IsUnique();

        // Index for daily count queries
        builder.HasIndex(x => new { x.SwiperId, x.CreatedAt });

        // Index for reverse swipe lookup (match detection)
        builder.HasIndex(x => new { x.SwipedId, x.SwiperId, x.Direction });
    }
}
