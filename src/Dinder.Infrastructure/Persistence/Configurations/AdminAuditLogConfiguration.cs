using Dinder.Domain.Entities;
using Dinder.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dinder.Infrastructure.Persistence.Configurations;

public sealed class AdminAuditLogConfiguration : IEntityTypeConfiguration<AdminAuditLog>
{
    public void Configure(EntityTypeBuilder<AdminAuditLog> builder)
    {
        builder.ToTable("audit_log");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Action)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.Timestamp)
            .IsRequired();

        // Indexes for audit trail queries
        builder.HasIndex(x => x.AdminId);
        builder.HasIndex(x => x.TargetUserId);
        builder.HasIndex(x => x.Timestamp);
        builder.HasIndex(x => new { x.Action, x.Timestamp });
    }
}
