using System.Text.Json;
using Microsoft.Extensions.Logging;
using TransactionAggregationAPI.Application.Domain;
using TransactionAggregationAPI.Application.Interfaces;

namespace TransactionAggregationAPI.Infrastructure.Sources.SourceA;


public class SourceAAdapter : ITransactionSource
{
    public string SourceName => "SourceA";

    private readonly ILogger<SourceAAdapter> _logger;

    public SourceAAdapter(ILogger<SourceAAdapter> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<UnifiedTransaction>> FetchTransactionsAsync(CancellationToken ct = default)
    {
        await Task.Delay(Random.Shared.Next(100, 400), ct);
        if (Random.Shared.NextDouble() < 0.1)
            throw new HttpRequestException("SourceA: simulated transient failure");

        var raw = GetMockData();
        return raw.Select(Map).ToList();
    }

    public static UnifiedTransaction Map(SourceATransaction src)
    {
        DateTime.TryParseExact(src.TransactionDate, "yyyy-MM-ddTHH:mm:ssZ",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal, out var date);
        return new UnifiedTransaction
        {
            ExternalId = src.TransactionId,
            SourceSystem = "SourceA",
            CustomerId = src.CustomerId,
            AccountId = src.AccountId,
            Amount = src.Amount,
            Currency = src.Currency,
            Description = src.Description,
            MerchantName = src.MerchantName,
            TransactionType = src.Type,
            TransactionDateUtc = DateTime.SpecifyKind(date, DateTimeKind.Utc),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            RawPayload = JsonSerializer.Serialize(src)
        };
    }
    private static List<SourceATransaction> GetMockData() =>
  [
        new() { TransactionId = "A001", CustomerId = "C001", AccountId = "ACC001", Amount = 250.00m, Currency = "ZAR", Description = "Checkers grocery shopping", MerchantName = "Checkers", Type = "debit", TransactionDate = "2025-01-15T10:30:00Z" },
        new() { TransactionId = "A002", CustomerId = "C001", AccountId = "ACC001", Amount = 45.50m, Currency = "ZAR", Description = "Uber trip to work", MerchantName = "Uber", Type = "debit", TransactionDate = "2025-01-16T08:15:00Z" },
        new() { TransactionId = "A003", CustomerId = "C002", AccountId = "ACC002", Amount = 15000.00m, Currency = "ZAR", Description = "Monthly salary deposit", MerchantName = "Employer Corp", Type = "credit", TransactionDate = "2025-01-25T00:00:00Z" },
        new() { TransactionId = "A004", CustomerId = "C002", AccountId = "ACC002", Amount = 899.00m, Currency = "ZAR", Description = "Netflix subscription", MerchantName = "Netflix", Type = "debit", TransactionDate = "2025-01-01T00:00:00Z" },
        new() { TransactionId = "A005", CustomerId = "C001", AccountId = "ACC001", Amount = 320.00m, Currency = "ZAR", Description = "Pick n Pay groceries", MerchantName = "Pick n Pay", Type = "debit", TransactionDate = "2025-01-18T14:20:00Z" },
        new() { TransactionId = "A006", CustomerId = "C003", AccountId = "ACC003", Amount = 2500.00m, Currency = "ZAR", Description = "Rent payment January", MerchantName = "City Property", Type = "debit", TransactionDate = "2025-01-01T09:00:00Z" },
        new() { TransactionId = "A007", CustomerId = "C003", AccountId = "ACC003", Amount = 150.00m, Currency = "ZAR", Description = "Dis-Chem pharmacy", MerchantName = "Dis-Chem", Type = "debit", TransactionDate = "2025-01-10T11:00:00Z" },
        new() { TransactionId = "A008", CustomerId = "C001", AccountId = "ACC001", Amount = 89.99m, Currency = "ZAR", Description = "Spotify premium", MerchantName = "Spotify", Type = "debit", TransactionDate = "2025-01-05T00:00:00Z" },
        new() { TransactionId = "A009", CustomerId = "C002", AccountId = "ACC002", Amount = 500.00m, Currency = "ZAR", Description = "EFT transfer to savings", MerchantName = "FNB Transfer", Type = "debit", TransactionDate = "2025-01-20T10:00:00Z" },
        new() { TransactionId = "A010", CustomerId = "C001", AccountId = "ACC001", Amount = 75.00m, Currency = "ZAR", Description = "BP Petrol fill-up", MerchantName = "BP", Type = "debit", TransactionDate = "2025-01-22T16:00:00Z" },
    ];
}
