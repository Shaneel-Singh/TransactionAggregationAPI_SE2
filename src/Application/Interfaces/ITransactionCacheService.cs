using TransactionAggregationAPI.Application.Domain;

namespace TransactionAggregationAPI.Application.Interfaces;

public interface ITransactionCacheService
{
    Task<UnifiedTransaction?> GetTransactionAsync(Guid id, CancellationToken ct = default);
    Task SetTransactionAsync(UnifiedTransaction transaction, CancellationToken ct = default);
    Task InvalidateTransactionAsync(Guid id, CancellationToken ct = default);
    Task InvalidateCustomerListAsync(string customerId, CancellationToken ct = default);
    Task InvalidateSummaryAsync(string customerId, CancellationToken ct = default);
    Task<CacheStats> GetStatsAsync();
}

public record CacheStats(long Hits, long Misses, long Invalidations);
