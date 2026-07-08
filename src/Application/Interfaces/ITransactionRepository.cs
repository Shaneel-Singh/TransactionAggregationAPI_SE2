using TransactionAggregationAPI.Application.Domain;

namespace TransactionAggregationAPI.Application.Interfaces;

public interface ITransactionRepository
{
    Task<UnifiedTransaction?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<UnifiedTransaction?> GetByExternalIdAndSourceAsync(string externalId, string sourceSystem, CancellationToken ct = default);
    Task<(IReadOnlyList<UnifiedTransaction> Items, int TotalCount)> GetByCustomerIdAsync(
        string customerId,
        int page,
        int pageSize,
        string? sortBy,
        string? sortOrder,
        string? category,
        DateTime? from,
        DateTime? to,
        CancellationToken ct = default);
    Task<(IReadOnlyList<UnifiedTransaction> Items, int TotalCount)> GetAllAsync(
        int page,
        int pageSize,
        string? sortBy,
        string? sortOrder,
        string? category,
        DateTime? from,
        DateTime? to,
        CancellationToken ct = default);
    Task<UnifiedTransaction> CreateAsync(UnifiedTransaction transaction, CancellationToken ct = default);
    Task<UnifiedTransaction> UpdateAsync(UnifiedTransaction transaction, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(Guid id, string deletedBy, CancellationToken ct = default);
    Task UpsertRangeAsync(IEnumerable<UnifiedTransaction> transactions, CancellationToken ct = default);
    Task<Dictionary<TransactionCategory, decimal>> GetCategoryTotalsAsync(string customerId, DateTime? from, DateTime? to, CancellationToken ct = default);
    Task<TransactionSummary> GetSummaryAsync(string customerId, DateTime? from, DateTime? to, CancellationToken ct = default);
}

public record TransactionSummary(
    string CustomerId,
    int TotalCount,
    decimal TotalAmount,
    decimal AverageAmount,
    decimal MaxAmount,
    decimal MinAmount,
    Dictionary<TransactionCategory, int> CountByCategory,
    Dictionary<TransactionCategory, decimal> AmountByCategory,
    DateTime? EarliestTransaction,
    DateTime? LatestTransaction);
