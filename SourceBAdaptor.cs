using System.Text.Json;
using Microsoft.Extensions.Logging;
using TransactionAggregationAPI.Application.Domain;
using TransactionAggregationAPI.Application.Interfaces;

namespace TransactionAggregationAPI.Infrastructure.Sources.SourceB;

public class SourceBAdapter : ITransactionSource
{
    public string SourceName => "SourceB";
    private readonly ILogger<SourceBAdapter> _logger;

    public SourceBAdapter(ILogger<SourceBAdapter> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<UnifiedTransaction>> FetchTransactionsAsync(CancellationToken ct = default)
    {
        await Task.Delay(Random.Shared.Next(200, 600), ct);

        if (Random.Shared.NextDouble() < 0.1)
            throw new HttpRequestException("SourceB: simulated transient failure");

        return GetMockData().Select(Map).ToList();
    }


    public static UnifiedTransaction Map(SourceBTransaction src)
    {
        var date = DateTimeOffset.FromUnixTimeSeconds(src.Metadata.UnixTimestamp).UtcDateTime;
        return new UnifiedTransaction
        {
            ExternalId = src.Id,
            SourceSystem = "SourceB",
            CustomerId = src.Customer.Ref,
            AccountId = src.Account.Number,
            Amount = src.Financial.Value,
            Currency = src.Financial.Ccy,
            Description = src.Financial.Narrative,
            MerchantName = src.Financial.Merchant,
            TransactionType = src.Financial.TxType,
            TransactionDateUtc = date,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            RawPayload = JsonSerializer.Serialize(src)
        };
    }

    private static List<SourceBTransaction> GetMockData() =>
    [
        new() { Id = "B001", Customer = new() { Ref = "C001" }, Account = new() { Number = "ACC001" }, Financial = new() { Value = 1200.00m, Ccy = "ZAR", Narrative = "Woolworths clothing purchase", Merchant = "Woolworths", TxType = "debit" }, Metadata = new() { UnixTimestamp = 1737000000 } },
        new() { Id = "B002", Customer = new() { Ref = "C002" }, Account = new() { Number = "ACC002" }, Financial = new() { Value = 350.00m, Ccy = "ZAR", Narrative = "Mediclinic consultation fee", Merchant = "Mediclinic", TxType = "debit" }, Metadata = new() { UnixTimestamp = 1737086400 } },
        new() { Id = "B003", Customer = new() { Ref = "C003" }, Account = new() { Number = "ACC003" }, Financial = new() { Value = 18000.00m, Ccy = "ZAR", Narrative = "Salary January 2025", Merchant = "Corp Ltd", TxType = "credit" }, Metadata = new() { UnixTimestamp = 1737705600 } },
        new() { Id = "B004", Customer = new() { Ref = "C001" }, Account = new() { Number = "ACC001" }, Financial = new() { Value = 125.00m, Ccy = "ZAR", Narrative = "Bolt ride to airport", Merchant = "Bolt", TxType = "debit" }, Metadata = new() { UnixTimestamp = 1737172800 } },
        new() { Id = "B005", Customer = new() { Ref = "C002" }, Account = new() { Number = "ACC002" }, Financial = new() { Value = 2500.00m, Ccy = "ZAR", Narrative = "University tuition payment", Merchant = "University of Cape Town", TxType = "debit" }, Metadata = new() { UnixTimestamp = 1737259200 } },
        new() { Id = "B006", Customer = new() { Ref = "C004" }, Account = new() { Number = "ACC004" }, Financial = new() { Value = 199.00m, Ccy = "ZAR", Narrative = "DStv premium subscription", Merchant = "MultiChoice", TxType = "debit" }, Metadata = new() { UnixTimestamp = 1737345600 } },
        new() { Id = "B007", Customer = new() { Ref = "C004" }, Account = new() { Number = "ACC004" }, Financial = new() { Value = 850.00m, Ccy = "ZAR", Narrative = "Engen petrol", Merchant = "Engen", TxType = "debit" }, Metadata = new() { UnixTimestamp = 1737432000 } },
        new() { Id = "B008", Customer = new() { Ref = "C003" }, Account = new() { Number = "ACC003" }, Financial = new() { Value = 3500.00m, Ccy = "ZAR", Narrative = "Flysafair ticket JHB-CPT", Merchant = "FlySafair", TxType = "debit" }, Metadata = new() { UnixTimestamp = 1737518400 } },
        new() { Id = "B009", Customer = new() { Ref = "C001" }, Account = new() { Number = "ACC001" }, Financial = new() { Value = 45.00m, Ccy = "ZAR", Narrative = "Bank service fee", Merchant = "FNB", TxType = "fee" }, Metadata = new() { UnixTimestamp = 1737604800 } },
        new() { Id = "B010", Customer = new() { Ref = "C002" }, Account = new() { Number = "ACC002" }, Financial = new() { Value = 980.00m, Ccy = "ZAR", Narrative = "Old Mutual insurance premium", Merchant = "Old Mutual", TxType = "debit" }, Metadata = new() { UnixTimestamp = 1737691200 } },
    ];
}
