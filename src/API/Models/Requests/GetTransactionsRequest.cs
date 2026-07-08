namespace TransactionAggregationAPI.API.Models.Requests;

public class GetTransactionsRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
    public string? SortOrder { get; set; } = "desc";
    public string? Category { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}
