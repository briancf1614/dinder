using Dinder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dinder.Infrastructure.Persistence.Configurations;

public sealed class BlockConfiguration : IEntityTypeConfiguration<Block>
{
    public void Configure(EntityTypeBuilder<Block> builder)
    {
        builder.ToTable("blocks");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // Composite unique index for block relationship
        builder.HasIndex(x => new { x.BlockerId, x.BlockedId })
            .IsUnique();

        // Index for checking blocked relationships
        builder.HasIndex(x => x.BlockedId);
    }
}
