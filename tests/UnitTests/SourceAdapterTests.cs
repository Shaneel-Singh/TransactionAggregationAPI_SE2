using FluentAssertions;
using TransactionAggregationAPI.Infrastructure.Sources.SourceA;
using TransactionAggregationAPI.Infrastructure.Sources.SourceB;
using TransactionAggregationAPI.Infrastructure.Sources.SourceC;
using Xunit;

namespace TransactionAggregationAPI.UnitTests;

public class SourceAdapterTests
{
    [Fact]
    public void SourceAAdapter_MapsAllFieldsCorrectly()
    {
        var source = new SourceATransaction
        {
            TransactionId = "A001",
            CustomerId = "C001",
            AccountId = "ACC001",
            Amount = 250.00m,
            Currency = "ZAR",
            Description = "Test transaction",
            MerchantName = "Test Merchant",
            Type = "debit",
            TransactionDate = "2025-01-15T10:30:00Z"
        };

        var result = SourceAAdapter.Map(source);

        result.ExternalId.Should().Be("A001");
        result.SourceSystem.Should().Be("SourceA");
        result.CustomerId.Should().Be("C001");
        result.AccountId.Should().Be("ACC001");
        result.Amount.Should().Be(250.00m);
        result.Currency.Should().Be("ZAR");
        result.Description.Should().Be("Test transaction");
        result.MerchantName.Should().Be("Test Merchant");
        result.TransactionType.Should().Be("debit");
        result.TransactionDateUtc.Year.Should().Be(2025);
        result.TransactionDateUtc.Month.Should().Be(1);
        result.TransactionDateUtc.Day.Should().Be(15);
        result.RawPayload.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void SourceAAdapter_ParsesIsoDateCorrectly()
    {
        var source = new SourceATransaction
        {
            TransactionId = "A001",
            CustomerId = "C001",
            AccountId = "ACC001",
            Amount = 100m,
            TransactionDate = "2025-03-19T14:25:30Z"
        };

        var result = SourceAAdapter.Map(source);

        result.TransactionDateUtc.Year.Should().Be(2025);
        result.TransactionDateUtc.Month.Should().Be(3);
        result.TransactionDateUtc.Day.Should().Be(19);
        result.TransactionDateUtc.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void SourceAAdapter_HandlesEmptyStrings()
    {
        var source = new SourceATransaction
        {
            TransactionId = "A001",
            CustomerId = "C001",
            AccountId = "ACC001",
            Amount = 0m,
            Currency = "",
            Description = "",
            MerchantName = "",
            Type = "",
            TransactionDate = "2025-01-01T00:00:00Z"
        };

        var result = SourceAAdapter.Map(source);

        result.Currency.Should().BeEmpty();
        result.Description.Should().BeEmpty();
        result.MerchantName.Should().BeEmpty();
        result.TransactionType.Should().BeEmpty();
    }

    [Fact]
    public void SourceBAdapter_MapsAllFieldsCorrectly()
    {
        var source = new SourceBTransaction
        {
            Id = "B001",
            Customer = new SourceBCustomer { Ref = "C001" },
            Account = new SourceBAccount { Number = "ACC001" },
            Financial = new SourceBFinancial
            {
                Value = 1200.00m,
                Ccy = "ZAR",
                Narrative = "Test purchase",
                Merchant = "Test Merchant",
                TxType = "debit"
            },
            Metadata = new SourceBMetadata { UnixTimestamp = 1737000000 }
        };

        var result = SourceBAdapter.Map(source);

        result.ExternalId.Should().Be("B001");
        result.SourceSystem.Should().Be("SourceB");
        result.CustomerId.Should().Be("C001");
        result.AccountId.Should().Be("ACC001");
        result.Amount.Should().Be(1200.00m);
        result.Currency.Should().Be("ZAR");
        result.Description.Should().Be("Test purchase");
        result.MerchantName.Should().Be("Test Merchant");
        result.TransactionType.Should().Be("debit");
        result.TransactionDateUtc.Kind.Should().Be(DateTimeKind.Utc);
        result.RawPayload.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void SourceBAdapter_ConvertsUnixTimestampToUtcDateTime()
    {
        var source = new SourceBTransaction
        {
            Id = "B001",
            Customer = new SourceBCustomer { Ref = "C001" },
            Account = new SourceBAccount { Number = "ACC001" },
            Financial = new SourceBFinancial { Value = 100m, Ccy = "ZAR" },
            Metadata = new SourceBMetadata { UnixTimestamp = 1609459200 } // 2021-01-01 00:00:00 UTC
        };

        var result = SourceBAdapter.Map(source);

        result.TransactionDateUtc.Year.Should().Be(2021);
        result.TransactionDateUtc.Month.Should().Be(1);
        result.TransactionDateUtc.Day.Should().Be(1);
        result.TransactionDateUtc.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void SourceBAdapter_HandlesNestedStructure()
    {
        var source = new SourceBTransaction
        {
            Id = "B001",
            Customer = new SourceBCustomer { Ref = "C999" },
            Account = new SourceBAccount { Number = "ACC999" },
            Financial = new SourceBFinancial
            {
                Value = 5000m,
                Ccy = "USD",
                Narrative = "Nested test",
                Merchant = "Nested Merchant",
                TxType = "credit"
            },
            Metadata = new SourceBMetadata { UnixTimestamp = 1700000000 }
        };

        var result = SourceBAdapter.Map(source);

        result.CustomerId.Should().Be("C999");
        result.AccountId.Should().Be("ACC999");
        result.Currency.Should().Be("USD");
        result.TransactionType.Should().Be("credit");
    }

    [Fact]
    public void SourceCAdapter_MapsAllFieldsCorrectly()
    {
        var source = new SourceCTransaction
        {
            Ref = "C001",
            ClientCode = "C001",
            AccountRef = "ACC001",
            TransactionValue = 3200.00m,
            CurrencyCode = "ZAR",
            TransactionNarrative = "Test transaction",
            Counterparty = "Test Counterparty",
            DebitCredit = "D",
            ValueDate = "18/01/2025"
        };

        var result = SourceCAdapter.Map(source);

        result.ExternalId.Should().Be("C001");
        result.SourceSystem.Should().Be("SourceC");
        result.CustomerId.Should().Be("C001");
        result.AccountId.Should().Be("ACC001");
        result.Amount.Should().Be(3200.00m);
        result.Currency.Should().Be("ZAR");
        result.Description.Should().Be("Test transaction");
        result.MerchantName.Should().Be("Test Counterparty");
        result.TransactionType.Should().Be("debit");
        result.TransactionDateUtc.Year.Should().Be(2025);
        result.TransactionDateUtc.Month.Should().Be(1);
        result.TransactionDateUtc.Day.Should().Be(18);
        result.RawPayload.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void SourceCAdapter_ParsesDdMmYyyyDateCorrectly()
    {
        var source = new SourceCTransaction
        {
            Ref = "C001",
            ClientCode = "C001",
            AccountRef = "ACC001",
            TransactionValue = 100m,
            CurrencyCode = "ZAR",
            DebitCredit = "D",
            ValueDate = "25/12/2024"
        };

        var result = SourceCAdapter.Map(source);

        result.TransactionDateUtc.Year.Should().Be(2024);
        result.TransactionDateUtc.Month.Should().Be(12);
        result.TransactionDateUtc.Day.Should().Be(25);
        result.TransactionDateUtc.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void SourceCAdapter_MapsDebitCreditD_ToDebit()
    {
        var source = new SourceCTransaction
        {
            Ref = "C001",
            ClientCode = "C001",
            AccountRef = "ACC001",
            TransactionValue = 100m,
            DebitCredit = "D",
            ValueDate = "01/01/2025"
        };

        var result = SourceCAdapter.Map(source);

        result.TransactionType.Should().Be("debit");
    }

    [Fact]
    public void SourceCAdapter_MapsDebitCreditC_ToCredit()
    {
        var source = new SourceCTransaction
        {
            Ref = "C001",
            ClientCode = "C001",
            AccountRef = "ACC001",
            TransactionValue = 100m,
            DebitCredit = "C",
            ValueDate = "01/01/2025"
        };

        var result = SourceCAdapter.Map(source);

        result.TransactionType.Should().Be("credit");
    }

    [Fact]
    public void SourceCAdapter_HandlesEmptyStrings()
    {
        var source = new SourceCTransaction
        {
            Ref = "C001",
            ClientCode = "C001",
            AccountRef = "ACC001",
            TransactionValue = 0m,
            CurrencyCode = "",
            TransactionNarrative = "",
            Counterparty = "",
            DebitCredit = "D",
            ValueDate = "01/01/2025"
        };

        var result = SourceCAdapter.Map(source);

        result.Currency.Should().BeEmpty();
        result.Description.Should().BeEmpty();
        result.MerchantName.Should().BeEmpty();
    }

    [Fact]
    public void SourceAAdapter_HandlesZeroAmount()
    {
        var source = new SourceATransaction
        {
            TransactionId = "A001",
            CustomerId = "C001",
            AccountId = "ACC001",
            Amount = 0m,
            TransactionDate = "2025-01-01T00:00:00Z"
        };

        var result = SourceAAdapter.Map(source);

        result.Amount.Should().Be(0m);
    }

    [Fact]
    public void SourceBAdapter_HandlesZeroAmount()
    {
        var source = new SourceBTransaction
        {
            Id = "B001",
            Customer = new SourceBCustomer { Ref = "C001" },
            Account = new SourceBAccount { Number = "ACC001" },
            Financial = new SourceBFinancial { Value = 0m },
            Metadata = new SourceBMetadata { UnixTimestamp = 1700000000 }
        };

        var result = SourceBAdapter.Map(source);

        result.Amount.Should().Be(0m);
    }

    [Fact]
    public void SourceCAdapter_HandlesFutureDates()
    {
        var source = new SourceCTransaction
        {
            Ref = "C001",
            ClientCode = "C001",
            AccountRef = "ACC001",
            TransactionValue = 100m,
            DebitCredit = "D",
            ValueDate = "31/12/2099"
        };

        var result = SourceCAdapter.Map(source);

        result.TransactionDateUtc.Year.Should().Be(2099);
        result.TransactionDateUtc.Month.Should().Be(12);
        result.TransactionDateUtc.Day.Should().Be(31);
    }
}
