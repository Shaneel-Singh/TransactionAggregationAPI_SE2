namespace TransactionAggregationAPI.Application.Domain;

public class UnifiedTransaction
{
    public Guid Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string SourceSystem { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "ZAR";
    public string Description { get; set; } = string.Empty;
    public string MerchantName { get; set; } = string.Empty;
    public TransactionCategory Category { get; set; } = TransactionCategory.Uncategorized;
    public string TransactionType { get; set; } = string.Empty;
    public DateTime TransactionDateUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = "system";
    public string UpdatedBy { get; set; } = "system";
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public string? RawPayload { get; set; }
}
