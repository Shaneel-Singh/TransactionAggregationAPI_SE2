using TransactionAggregationAPI.Application.Domain;

namespace TransactionAggregationAPI.Application.Interfaces;

public interface ICategorizationService
{
    TransactionCategory Categorize(string description, string merchantName, decimal amount);
    Task RecategorizeAllAsync(CancellationToken ct = default);
}
