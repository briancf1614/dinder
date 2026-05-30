using Dinder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dinder.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Token)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.RevokedAt);

        builder.Property(x => x.ReplacedByToken)
            .HasMaxLength(512);

        // Fast lookup by token value
        builder.HasIndex(x => x.Token)
            .IsUnique();

        // Cleanup expired tokens
        builder.HasIndex(x => x.ExpiresAt);
    }
}
