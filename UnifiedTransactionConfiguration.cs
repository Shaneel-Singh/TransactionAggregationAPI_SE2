using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransactionAggregationAPI.Application.Domain;

namespace TransactionAggregationAPI.Infrastructure.Data.Configurations;

public class UnifiedTransactionConfiguration : IEntityTypeConfiguration<UnifiedTransaction>
{
    public void Configure(EntityTypeBuilder<UnifiedTransaction> builder)
    {
        builder.ToTable("transactions");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.ExternalId).IsRequired().HasMaxLength(256);
        builder.Property(t => t.SourceSystem).IsRequired().HasMaxLength(64);
        builder.Property(t => t.CustomerId).IsRequired().HasMaxLength(64);
        builder.Property(t => t.AccountId).IsRequired().HasMaxLength(64);
        builder.Property(t => t.Amount).HasPrecision(18, 4);
        builder.Property(t => t.Currency).IsRequired().HasMaxLength(3);
        builder.Property(t => t.Description).HasMaxLength(512);
        builder.Property(t => t.MerchantName).HasMaxLength(256);
        builder.Property(t => t.TransactionType).HasMaxLength(64);
        builder.Property(t => t.Category).HasConversion<string>().HasMaxLength(64);
        builder.Property(t => t.CreatedBy).HasMaxLength(128);
        builder.Property(t => t.UpdatedBy).HasMaxLength(128);
        builder.Property(t => t.DeletedBy).HasMaxLength(128);

        // Unique constraint enforces idempotent upsert on (ExternalId, SourceSystem)
        builder.HasIndex(t => new { t.ExternalId, t.SourceSystem }).IsUnique().HasDatabaseName("ix_transactions_external_source");

        builder.HasIndex(t => t.CustomerId).HasDatabaseName("ix_transactions_customer_id");
        builder.HasIndex(t => t.AccountId).HasDatabaseName("ix_transactions_account_id");
        builder.HasIndex(t => t.TransactionDateUtc).HasDatabaseName("ix_transactions_date");
        builder.HasIndex(t => t.Category).HasDatabaseName("ix_transactions_category");
        builder.HasIndex(t => new { t.CustomerId, t.TransactionDateUtc }).HasDatabaseName("ix_transactions_customer_date");
        builder.HasIndex(t => new { t.CustomerId, t.Category }).HasDatabaseName("ix_transactions_customer_category");
    }
}
