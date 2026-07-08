namespace TransactionAggregationAPI.Application.Interfaces;

public interface IAggregationService
{
    Task<AggregationResult> AggregateAsync(CancellationToken ct = default);
}

public record AggregationResult(
    int TotalFetched,
    int TotalUpserted,
    IReadOnlyList<SourceResult> SourceResults);

public record SourceResult(
    string SourceName,
    bool Success,
    int Count,
    string? ErrorMessage,
    TimeSpan Duration);
