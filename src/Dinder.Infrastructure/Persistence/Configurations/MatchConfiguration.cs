using Dinder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dinder.Infrastructure.Persistence.Configurations;

public sealed class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.ToTable("matches");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId1)
            .IsRequired();

        builder.Property(x => x.UserId2)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // Ensure we store the pair consistently (smaller GUID first) doesn't matter for lookups
        // but we need to look up by either user
        builder.HasIndex(x => x.UserId1);
        builder.HasIndex(x => x.UserId2);

        // Composite unique for the pair
        builder.HasIndex(x => new { x.UserId1, x.UserId2 })
            .IsUnique();
    }
}
