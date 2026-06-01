using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dinder.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Body)
            .HasMaxLength(512);

        builder.Property(x => x.DeepLinkPayload)
            .HasMaxLength(1024);

        builder.Property(x => x.IsRead)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // Index for cursor-paginated list (user + created desc)
        builder.HasIndex(x => new { x.UserId, x.CreatedAt });
        builder.HasIndex(x => new { x.UserId, x.IsRead });
    }
}
