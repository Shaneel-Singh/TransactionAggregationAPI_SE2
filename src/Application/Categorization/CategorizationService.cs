using Microsoft.Extensions.Logging;
using TransactionAggregationAPI.Application.Domain;
using TransactionAggregationAPI.Application.Interfaces;

namespace TransactionAggregationAPI.Application.Categorization;

public class CategorizationService : ICategorizationService
{
    private readonly IReadOnlyList<ICategorizationStrategy> _strategies;
    private readonly ILogger<CategorizationService> _logger;

    public CategorizationService(
        IEnumerable<ICategorizationStrategy> strategies,
        ILogger<CategorizationService> logger)
    {
        _strategies = strategies.OrderBy(s => s.Priority).ToList();
        _logger = logger;
    }

    public TransactionCategory Categorize(string description, string merchantName, decimal amount)
    {
        foreach (var strategy in _strategies)
        {
            if (strategy.TryCategorize(description, merchantName, amount, out var category))
            {
                _logger.LogDebug("Categorized '{Description}' as {Category} via {Strategy}",
                    description, category, strategy.GetType().Name);
                return category;
            }
        }
        return TransactionCategory.Uncategorized;
    }

    public Task RecategorizeAllAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("RecategorizeAll requested");
        return Task.CompletedTask;
    }
}
