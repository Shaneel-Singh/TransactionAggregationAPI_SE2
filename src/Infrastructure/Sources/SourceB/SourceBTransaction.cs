using System.Text.Json.Serialization;

namespace TransactionAggregationAPI.Infrastructure.Sources.SourceB;

// Source B: nested JSON with Unix
public class SourceBTransaction
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("customer")] public SourceBCustomer Customer { get; set; } = new();
    [JsonPropertyName("account")] public SourceBAccount Account { get; set; } = new();
    [JsonPropertyName("financial")] public SourceBFinancial Financial { get; set; } = new();
    [JsonPropertyName("metadata")] public SourceBMetadata Metadata { get; set; } = new();
}

public class SourceBCustomer
{
    [JsonPropertyName("ref")] public string Ref { get; set; } = string.Empty;
}

public class SourceBAccount
{
    [JsonPropertyName("number")] public string Number { get; set; } = string.Empty;
}

public class SourceBFinancial
{
    [JsonPropertyName("value")] public decimal Value { get; set; }
    [JsonPropertyName("ccy")] public string Ccy { get; set; } = "ZAR";
    [JsonPropertyName("narrative")] public string Narrative { get; set; } = string.Empty;
    [JsonPropertyName("merchant")] public string Merchant { get; set; } = string.Empty;
    [JsonPropertyName("txType")] public string TxType { get; set; } = string.Empty;
}

public class SourceBMetadata
{
    [JsonPropertyName("unixTimestamp")] public long UnixTimestamp { get; set; }
}
