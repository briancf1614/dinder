using Dinder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dinder.Infrastructure.Persistence.Configurations;

public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("messages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ConversationId)
            .IsRequired();

        builder.Property(x => x.SenderId)
            .IsRequired();

        builder.Property(x => x.Content)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.SentAt)
            .IsRequired();

        builder.Property(x => x.ReadAt);

        // Index for cursor-paginated history (conversation + sent time)
        builder.HasIndex(x => new { x.ConversationId, x.SentAt });
        builder.HasIndex(x => x.ConversationId);
        builder.HasIndex(x => x.SenderId);
    }
}
