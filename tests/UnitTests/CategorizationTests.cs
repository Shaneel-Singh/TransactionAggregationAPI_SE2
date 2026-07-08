using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TransactionAggregationAPI.Application.Categorization;
using TransactionAggregationAPI.Application.Domain;
using TransactionAggregationAPI.Application.Interfaces;
using Xunit;

namespace TransactionAggregationAPI.UnitTests;

public class CategorizationTests
{
    private readonly Mock<ILogger<CategorizationService>> _mockLogger;
    private readonly KeywordCategorizationStrategy _strategy;

    public CategorizationTests()
    {
        _mockLogger = new Mock<ILogger<CategorizationService>>();
        _strategy = new KeywordCategorizationStrategy();
    }

    [Fact]
    public void Strategy_CategorizesFoodCorrectly()
    {
        var result = _strategy.TryCategorize("Checkers grocery shopping", "Checkers", 250m, out var category);
        result.Should().BeTrue();
        category.Should().Be(TransactionCategory.Food);
    }

    [Fact]
    public void Strategy_CategorizesTransportCorrectly()
    {
        var result = _strategy.TryCategorize("Uber trip to work", "Uber", 45m, out var category);
        result.Should().BeTrue();
        category.Should().Be(TransactionCategory.Transport);
    }

    [Fact]
    public void Strategy_CategorizesEntertainmentCorrectly()
    {
        var result = _strategy.TryCategorize("Netflix subscription", "Netflix", 199m, out var category);
        result.Should().BeTrue();
        category.Should().Be(TransactionCategory.Entertainment);
    }

    [Fact]
    public void Strategy_CategorizesHealthcareCorrectly()
    {
        var result = _strategy.TryCategorize("Pharmacy visit", "Clicks pharmacy", 150m, out var category);
        result.Should().BeTrue();
        category.Should().Be(TransactionCategory.Healthcare);
    }

    [Fact]
    public void Strategy_CategorizesShoppingCorrectly()
    {
        var result = _strategy.TryCategorize("Woolworths clothing purchase", "Woolworths", 1200m, out var category);
        result.Should().BeTrue();
        category.Should().Be(TransactionCategory.Shopping);
    }

    [Fact]
    public void Strategy_CategorizesUtilitiesCorrectly()
    {
        var result = _strategy.TryCategorize("Eskom electricity prepaid", "Eskom", 650m, out var category);
        result.Should().BeTrue();
        category.Should().Be(TransactionCategory.Utilities);
    }

    [Fact]
    public void Strategy_CategorizesTravelCorrectly()
    {
        var result = _strategy.TryCategorize("Airbnb Cape Town trip", "Airbnb", 2100m, out var category);
        result.Should().BeTrue();
        category.Should().Be(TransactionCategory.Travel);
    }

    [Fact]
    public void Strategy_CategorizesEducationCorrectly()
    {
        var result = _strategy.TryCategorize("University tuition payment", "University of Cape Town", 2500m, out var category);
        result.Should().BeTrue();
        category.Should().Be(TransactionCategory.Education);
    }

    [Fact]
    public void Strategy_CategorizesSalaryCorrectly()
    {
        var result = _strategy.TryCategorize("Monthly salary deposit", "Employer Corp", 15000m, out var category);
        result.Should().BeTrue();
        category.Should().Be(TransactionCategory.Salary);
    }

    [Fact]
    public void Strategy_CategorizesTransferCorrectly()
    {
        var result = _strategy.TryCategorize("EFT transfer to savings", "FNB Transfer", 500m, out var category);
        result.Should().BeTrue();
        category.Should().Be(TransactionCategory.Transfer);
    }

    [Fact]
    public void Strategy_CategorizesFeesCorrectly()
    {
        var result = _strategy.TryCategorize("Bank service fee", "FNB", 45m, out var category);
        result.Should().BeTrue();
        category.Should().Be(TransactionCategory.Fees);
    }

    [Fact]
    public void Strategy_CategorizesInsuranceCorrectly()
    {
        var result = _strategy.TryCategorize("Old Mutual insurance premium", "Old Mutual", 980m, out var category);
        result.Should().BeTrue();
        category.Should().Be(TransactionCategory.Insurance);
    }

    [Fact]
    public void Strategy_CategorizesHousingCorrectly()
    {
        var result = _strategy.TryCategorize("Rent payment January", "City Property", 2500m, out var category);
        result.Should().BeTrue();
        category.Should().Be(TransactionCategory.Housing);
    }

    [Fact]
    public void Strategy_ReturnsUncategorizedForUnknownDescription()
    {
        var result = _strategy.TryCategorize("Random unknown transaction", "Unknown Merchant", 100m, out var category);
        result.Should().BeFalse();
        category.Should().Be(TransactionCategory.Uncategorized);
    }

    [Fact]
    public void Strategy_IsCaseInsensitive()
    {
        var result = _strategy.TryCategorize("CHECKERS GROCERY SHOPPING", "CHECKERS", 250m, out var category);
        result.Should().BeTrue();
        category.Should().Be(TransactionCategory.Food);
    }

    [Fact]
    public void Strategy_MatchesKeywordInMerchantName()
    {
        var result = _strategy.TryCategorize("Purchase", "Pick n Pay", 320m, out var category);
        result.Should().BeTrue();
        category.Should().Be(TransactionCategory.Food);
    }

    [Fact]
    public void Strategy_MatchesPartialKeyword()
    {
        var result = _strategy.TryCategorize("Payment at Woolworths food section", "", 100m, out var category);
        result.Should().BeTrue();
        category.Should().Be(TransactionCategory.Food);
    }

    [Fact]
    public void CategorizationService_UsesStrategyCorrectly()
    {
        var strategies = new List<ICategorizationStrategy> { _strategy };
        var service = new CategorizationService(strategies, _mockLogger.Object);

        var category = service.Categorize("Netflix subscription", "Netflix", 199m);
        category.Should().Be(TransactionCategory.Entertainment);
    }

    [Fact]
    public void CategorizationService_ReturnsUncategorizedWhenNoStrategyMatches()
    {
        var strategies = new List<ICategorizationStrategy> { _strategy };
        var service = new CategorizationService(strategies, _mockLogger.Object);

        var category = service.Categorize("Unknown transaction", "Unknown", 100m);
        category.Should().Be(TransactionCategory.Uncategorized);
    }

    [Fact]
    public void CategorizationService_UsesLowestPriorityStrategyFirst()
    {
        var mockStrategy1 = new Mock<ICategorizationStrategy>();
        mockStrategy1.Setup(s => s.Priority).Returns(20);
        mockStrategy1.Setup(s => s.TryCategorize(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), out It.Ref<TransactionCategory>.IsAny))
            .Returns(new TryCategorizeDelegate((string desc, string merchant, decimal amt, out TransactionCategory cat) =>
            {
                cat = TransactionCategory.Other;
                return true;
            }));

        var mockStrategy2 = new Mock<ICategorizationStrategy>();
        mockStrategy2.Setup(s => s.Priority).Returns(10);
        mockStrategy2.Setup(s => s.TryCategorize(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), out It.Ref<TransactionCategory>.IsAny))
            .Returns(new TryCategorizeDelegate((string desc, string merchant, decimal amt, out TransactionCategory cat) =>
            {
                cat = TransactionCategory.Food;
                return true;
            }));

        var strategies = new List<ICategorizationStrategy> { mockStrategy1.Object, mockStrategy2.Object };
        var service = new CategorizationService(strategies, _mockLogger.Object);

        var category = service.Categorize("test", "test", 100m);
        category.Should().Be(TransactionCategory.Food); // Strategy with priority 10 should win
    }

    [Fact]
    public void Strategy_MatchesMcDonalds()
    {
        var result = _strategy.TryCategorize("Meal at McDonalds", "McDonalds", 89m, out var category);
        result.Should().BeTrue();
        category.Should().Be(TransactionCategory.Food);
    }

    [Fact]
    public void Strategy_MatchesSpotify()
    {
        var result = _strategy.TryCategorize("Spotify premium", "Spotify", 89.99m, out var category);
        result.Should().BeTrue();
        category.Should().Be(TransactionCategory.Entertainment);
    }

    [Fact]
    public void Strategy_MatchesPetrol()
    {
        var result = _strategy.TryCategorize("BP Petrol fill-up", "BP", 750m, out var category);
        result.Should().BeTrue();
        category.Should().Be(TransactionCategory.Transport);
    }

    [Fact]
    public void Strategy_MatchesVodacom()
    {
        var result = _strategy.TryCategorize("Vodacom data bundle", "Vodacom", 180m, out var category);
        result.Should().BeTrue();
        category.Should().Be(TransactionCategory.Utilities);
    }

    private delegate bool TryCategorizeDelegate(string description, string merchantName, decimal amount, out TransactionCategory category);
}
