using System.Text.Json.Serialization;

namespace TransactionAggregationAPI.Infrastructure.Sources.SourceA;

// Source A: flat JSON with camelCase, ISO 8601 dates
public class SourceATransaction
{
    [JsonPropertyName("transactionId")] public string TransactionId { get; set; } = string.Empty;
    [JsonPropertyName("customerId")] public string CustomerId { get; set; } = string.Empty;
    [JsonPropertyName("accountId")] public string AccountId { get; set; } = string.Empty;
    [JsonPropertyName("amount")] public decimal Amount { get; set; }
    [JsonPropertyName("currency")] public string Currency { get; set; } = "ZAR";
    [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;
    [JsonPropertyName("merchantName")] public string MerchantName { get; set; } = string.Empty;
    [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;
    [JsonPropertyName("transactionDate")] public string TransactionDate { get; set; } = string.Empty;
}