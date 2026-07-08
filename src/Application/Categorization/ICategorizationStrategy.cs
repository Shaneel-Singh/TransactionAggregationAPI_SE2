using TransactionAggregationAPI.Application.Domain;

namespace TransactionAggregationAPI.Application.Categorization;

public interface ICategorizationStrategy
{
    bool TryCategorize(string description, string merchantName, decimal amount, out TransactionCategory category);
    int Priority { get; }
}
