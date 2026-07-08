namespace TransactionAggregationAPI.API.Models.Responses;

public class SummaryResponse
{
    public string CustomerId { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AverageAmount { get; set; }
    public decimal MaxAmount { get; set; }
    public decimal MinAmount { get; set; }
    public Dictionary<string, int> CountByCategory { get; set; } = new();
    public Dictionary<string, decimal> AmountByCategory { get; set; } = new();
    public DateTime? EarliestTransaction { get; set; }
    public DateTime? LatestTransaction { get; set; }
}
