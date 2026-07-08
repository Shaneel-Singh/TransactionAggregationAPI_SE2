using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TransactionAggregationAPI.Application.Domain;
using TransactionAggregationAPI.Application.Interfaces;

namespace TransactionAggregationAPI.Infrastructure.Sources.SourceC;

public class SourceCAdapter : ITransactionSource
{
    public string SourceName => "SourceC";
    private readonly ILogger<SourceCAdapter> _logger;

    public SourceCAdapter(ILogger<SourceCAdapter> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<UnifiedTransaction>> FetchTransactionsAsync(CancellationToken ct = default)
    {
        await Task.Delay(Random.Shared.Next(300, 800), ct);

        if (Random.Shared.NextDouble() < 0.1)
            throw new HttpRequestException("SourceC: simulated transient failure");

        return GetMockData().Select(Map).ToList();
    }

    public static UnifiedTransaction Map(SourceCTransaction src)
    {
        DateTime.TryParseExact(src.ValueDate, "dd/MM/yyyy", CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal, out var date);
        var txType = src.DebitCredit == "C" ? "credit" : "debit";
        return new UnifiedTransaction
        {
            ExternalId = src.Ref,
            SourceSystem = "SourceC",
            CustomerId = src.ClientCode,
            AccountId = src.AccountRef,
            Amount = src.TransactionValue,
            Currency = src.CurrencyCode,
            Description = src.TransactionNarrative,
            MerchantName = src.Counterparty,
            TransactionType = txType,
            TransactionDateUtc = date.ToUniversalTime(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            RawPayload = JsonSerializer.Serialize(src)
        };
    }

    private static List<SourceCTransaction> GetMockData() =>
    [
        new() { Ref = "C001", ClientCode = "C001", AccountRef = "ACC001", TransactionValue = 3200.00m, CurrencyCode = "ZAR", TransactionNarrative = "Makro wholesale shopping", Counterparty = "Makro", DebitCredit = "D", ValueDate = "18/01/2025" },
        new() { Ref = "C002", ClientCode = "C003", AccountRef = "ACC003", TransactionValue = 12000.00m, CurrencyCode = "ZAR", TransactionNarrative = "Freelance payment received", Counterparty = "Client XYZ", DebitCredit = "C", ValueDate = "20/01/2025" },
        new() { Ref = "C003", ClientCode = "C004", AccountRef = "ACC004", TransactionValue = 450.00m, CurrencyCode = "ZAR", TransactionNarrative = "Mediclinic hospital stay", Counterparty = "Mediclinic Morningside", DebitCredit = "D", ValueDate = "08/01/2025" },
        new() { Ref = "C004", ClientCode = "C002", AccountRef = "ACC002", TransactionValue = 2100.00m, CurrencyCode = "ZAR", TransactionNarrative = "Airbnb Cape Town trip", Counterparty = "Airbnb", DebitCredit = "D", ValueDate = "22/01/2025" },
        new() { Ref = "C005", ClientCode = "C001", AccountRef = "ACC001", TransactionValue = 180.00m, CurrencyCode = "ZAR", TransactionNarrative = "Vodacom data bundle", Counterparty = "Vodacom", DebitCredit = "D", ValueDate = "02/01/2025" },
        new() { Ref = "C006", ClientCode = "C003", AccountRef = "ACC003", TransactionValue = 750.00m, CurrencyCode = "ZAR", TransactionNarrative = "Foschini clothing", Counterparty = "Foschini", DebitCredit = "D", ValueDate = "12/01/2025" },
        new() { Ref = "C007", ClientCode = "C004", AccountRef = "ACC004", TransactionValue = 290.00m, CurrencyCode = "ZAR", TransactionNarrative = "KFC family meal", Counterparty = "KFC", DebitCredit = "D", ValueDate = "14/01/2025" },
        new() { Ref = "C008", ClientCode = "C002", AccountRef = "ACC002", TransactionValue = 1800.00m, CurrencyCode = "ZAR", TransactionNarrative = "Sanlam insurance premium", Counterparty = "Sanlam", DebitCredit = "D", ValueDate = "01/01/2025" },
        new() { Ref = "C009", ClientCode = "C001", AccountRef = "ACC001", TransactionValue = 650.00m, CurrencyCode = "ZAR", TransactionNarrative = "Eskom electricity prepaid", Counterparty = "Eskom", DebitCredit = "D", ValueDate = "10/01/2025" },
        new() { Ref = "C010", ClientCode = "C003", AccountRef = "ACC003", TransactionValue = 220.00m, CurrencyCode = "ZAR", TransactionNarrative = "Steers burger restaurant", Counterparty = "Steers", DebitCredit = "D", ValueDate = "16/01/2025" },
    ];
}
