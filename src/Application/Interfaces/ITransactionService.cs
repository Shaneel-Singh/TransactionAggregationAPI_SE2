using TransactionAggregationAPI.Application.Domain;

namespace TransactionAggregationAPI.Application.Interfaces;

public interface ITransactionService
{
    Task<UnifiedTransaction?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<(IReadOnlyList<UnifiedTransaction> Items, int TotalCount)> GetByCustomerAsync(
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

    Task<UnifiedTransaction> CreateAsync(
        string customerId,
        string accountId,
        decimal amount,
        string currency,
        string description,
        string merchantName,
        string transactionType,
        DateTime transactionDateUtc,
        string createdBy,
        CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid id, string deletedBy, CancellationToken ct = default);

    Task<TransactionSummary> GetSummaryAsync(
        string customerId,
        DateTime? from,
        DateTime? to,
        CancellationToken ct = default);
}
