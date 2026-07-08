using Microsoft.EntityFrameworkCore;
using TransactionAggregationAPI.Application.Domain;

namespace TransactionAggregationAPI.Infrastructure.Data;

public class TransactionDbContext : DbContext
{
    public TransactionDbContext(DbContextOptions<TransactionDbContext> options) : base(options) { }

    public DbSet<UnifiedTransaction> Transactions => Set<UnifiedTransaction>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TransactionDbContext).Assembly);

        // Global soft-delete filter
        modelBuilder.Entity<UnifiedTransaction>().HasQueryFilter(t => !t.IsDeleted);
    }
}
