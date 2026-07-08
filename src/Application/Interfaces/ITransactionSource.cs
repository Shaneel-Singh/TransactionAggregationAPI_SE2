using TransactionAggregationAPI.Application.Domain;

namespace TransactionAggregationAPI.Application.Interfaces;

public interface ITransactionSource
{
    string SourceName { get; }

    Task<IReadOnlyList<UnifiedTransaction>> FetchTransactionsAsync(CancellationToken ct = default);
}