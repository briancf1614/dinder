using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dinder.Infrastructure.Persistence.Configurations;

public sealed class PromptCatalogConfiguration : IEntityTypeConfiguration<PromptCatalog>
{
    public void Configure(EntityTypeBuilder<PromptCatalog> builder)
    {
        builder.ToTable("prompt_catalog");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Text)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Category)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.IsEnabled)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => x.Category);
        builder.HasIndex(x => x.IsEnabled);
    }
}
