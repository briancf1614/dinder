using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
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

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.UnmatchedByUserId);

        builder.Property(x => x.UnmatchedAt);

        builder.Property(x => x.IcebreakerQuestion)
            .HasMaxLength(150);

        builder.Property(x => x.IcebreakerCategory)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => x.MatchId)
            .IsUnique();

        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.Match)
            .WithOne(x => x.Conversation)
            .HasForeignKey<Conversation>(x => x.MatchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
