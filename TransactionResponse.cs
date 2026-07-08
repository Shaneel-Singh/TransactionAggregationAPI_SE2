using TransactionAggregationAPI.Application.Domain;

namespace TransactionAggregationAPI.API.Models.Responses;

public class TransactionResponse
{
    public Guid Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string SourceSystem { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string MerchantName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public DateTime TransactionDateUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public static TransactionResponse FromDomain(UnifiedTransaction t) => new()
    {
        Id = t.Id,
        ExternalId = t.ExternalId,
        SourceSystem = t.SourceSystem,
        CustomerId = t.CustomerId,
        AccountId = t.AccountId,
        Amount = t.Amount,
        Currency = t.Currency,
        Description = t.Description,
        MerchantName = t.MerchantName,
        Category = t.Category.ToString(),
        TransactionType = t.TransactionType,
        TransactionDateUtc = t.TransactionDateUtc,
        CreatedAtUtc = t.CreatedAtUtc
    };
}
