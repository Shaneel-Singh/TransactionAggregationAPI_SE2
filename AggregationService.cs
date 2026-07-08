using Microsoft.Extensions.Logging;
using TransactionAggregationAPI.Application.Domain;
using TransactionAggregationAPI.Application.Interfaces;

namespace TransactionAggregationAPI.Application.Services;

public class AggregationService : IAggregationService
{
    private readonly IEnumerable<ITransactionSource> _sources;
    private readonly ITransactionRepository _repository;
    private readonly ICategorizationService _categorization;
    private readonly ILogger<AggregationService> _logger;

    public AggregationService(
        IEnumerable<ITransactionSource> sources,
        ITransactionRepository repository,
        ICategorizationService categorization,
        ILogger<AggregationService> logger)
    {
        _sources = sources;
        _repository = repository;
        _categorization = categorization;
        _logger = logger;
    }

    public async Task<AggregationResult> AggregateAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting aggregation from {Count} sources", _sources.Count());

        var tasks = _sources.Select(source => FetchFromSourceAsync(source, ct)).ToList();
        var sourceResults = await Task.WhenAll(tasks);

        var allTransactions = sourceResults
            .Where(r => r.Success)
            .SelectMany(r => r.Transactions ?? [])
            .ToList();

        foreach (var tx in allTransactions)
        {
            if (tx.Category == TransactionCategory.Uncategorized)
                tx.Category = _categorization.Categorize(tx.Description, tx.MerchantName, tx.Amount);
        }

        int upserted = 0;
        if (allTransactions.Count > 0)
        {
            await _repository.UpsertRangeAsync(allTransactions, ct);
            upserted = allTransactions.Count;
        }

        var results = sourceResults.Select(r => new SourceResult(
            r.SourceName,
            r.Success,
            r.Transactions?.Count ?? 0,
            r.ErrorMessage,
            r.Duration)).ToList();

        _logger.LogInformation("Aggregation complete. Fetched={Fetched} Upserted={Upserted} SourcesFailed={Failed}",
            allTransactions.Count, upserted, results.Count(r => !r.Success));

        return new AggregationResult(allTransactions.Count, upserted, results);
    }

    private async Task<InternalSourceResult> FetchFromSourceAsync(ITransactionSource source, CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var transactions = await source.FetchTransactionsAsync(ct);
            sw.Stop();
            _logger.LogInformation("Source {Source} returned {Count} transactions in {Ms}ms",
                source.SourceName, transactions.Count, sw.ElapsedMilliseconds);
            return new InternalSourceResult(source.SourceName, true, transactions.ToList(), null, sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Source {Source} failed after {Ms}ms", source.SourceName, sw.ElapsedMilliseconds);
            return new InternalSourceResult(source.SourceName, false, null, ex.Message, sw.Elapsed);
        }
    }

    private record InternalSourceResult(
        string SourceName,
        bool Success,
        List<UnifiedTransaction>? Transactions,
        string? ErrorMessage,
        TimeSpan Duration);
}
