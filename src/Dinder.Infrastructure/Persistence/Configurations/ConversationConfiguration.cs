using Dinder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dinder.Infrastructure.Persistence.Configurations;

public sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("conversations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.MatchId)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => x.MatchId)
            .IsUnique();

        builder.HasOne(x => x.Match)
            .WithOne(x => x.Conversation)
            .HasForeignKey<Conversation>(x => x.MatchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
