using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransactionAggregationAPI.Application.Domain;

namespace TransactionAggregationAPI.Infrastructure.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.EntityName).IsRequired().HasMaxLength(128);
        builder.Property(a => a.Action).IsRequired().HasMaxLength(64);
        builder.Property(a => a.PerformedBy).IsRequired().HasMaxLength(128);
        builder.Property(a => a.CorrelationId).HasMaxLength(128);
        builder.HasIndex(a => a.EntityId).HasDatabaseName("ix_audit_logs_entity_id");
        builder.HasIndex(a => a.PerformedAtUtc).HasDatabaseName("ix_audit_logs_performed_at");
    }
}
