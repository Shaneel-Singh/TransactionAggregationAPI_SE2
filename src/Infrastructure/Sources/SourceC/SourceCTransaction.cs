namespace TransactionAggregationAPI.Infrastructure.Sources.SourceC;

// Source C: CSV-like structure with different field names
public class SourceCTransaction
{
    public string Ref { get; set; } = string.Empty;
    public string ClientCode { get; set; } = string.Empty;
    public string AccountRef { get; set; } = string.Empty;
    public decimal TransactionValue { get; set; }
    public string CurrencyCode { get; set; } = "ZAR";
    public string TransactionNarrative { get; set; } = string.Empty;
    public string Counterparty { get; set; } = string.Empty;
    public string DebitCredit { get; set; } = string.Empty;  // "D" or "C"
    public string ValueDate { get; set; } = string.Empty;    // dd/MM/yyyy format
}
