namespace TransactionAggregationAPI.API.Models.Responses;

public class AggregationResponse
{
    public int TotalFetched { get; set; }
    public int TotalUpserted { get; set; }
    public IReadOnlyList<SourceResultResponse> SourceResults { get; set; } = [];
}

public class SourceResultResponse
{
    public string SourceName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int Count { get; set; }
    public string? ErrorMessage { get; set; }
    public double DurationMs { get; set; }
}
