using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TransactionAggregationAPI.Application.Domain;
using TransactionAggregationAPI.Application.Interfaces;
using TransactionAggregationAPI.Application.Services;
using Xunit;

namespace TransactionAggregationAPI.UnitTests;

public class AggregationServiceTests
{
    private readonly Mock<ITransactionRepository> _mockRepository;
    private readonly Mock<ICategorizationService> _mockCategorization;
    private readonly Mock<ILogger<AggregationService>> _mockLogger;

    public AggregationServiceTests()
    {
        _mockRepository = new Mock<ITransactionRepository>();
        _mockCategorization = new Mock<ICategorizationService>();
        _mockLogger = new Mock<ILogger<AggregationService>>();
    }

    [Fact]
    public async Task AggregateAsync_WithAllSourcesSucceeding_ReturnsTotalFetched()
    {
        var mockSource1 = CreateMockSource("SourceA", 3);
        var mockSource2 = CreateMockSource("SourceB", 5);
        var mockSource3 = CreateMockSource("SourceC", 2);

        var sources = new[] { mockSource1.Object, mockSource2.Object, mockSource3.Object };
        var service = new AggregationService(sources, _mockRepository.Object, _mockCategorization.Object, _mockLogger.Object);

        var result = await service.AggregateAsync();

        result.TotalFetched.Should().Be(10);
        result.TotalUpserted.Should().Be(10);
        result.SourceResults.Should().HaveCount(3);
        result.SourceResults.Should().AllSatisfy(r => r.Success.Should().BeTrue());
    }

    [Fact]
    public async Task AggregateAsync_WithOneSourceFailing_ReturnsPartialResults()
    {
        var mockSource1 = CreateMockSource("SourceA", 3);
        var mockSource2 = CreateFailingMockSource("SourceB", "Connection timeout");
        var mockSource3 = CreateMockSource("SourceC", 2);

        var sources = new[] { mockSource1.Object, mockSource2.Object, mockSource3.Object };
        var service = new AggregationService(sources, _mockRepository.Object, _mockCategorization.Object, _mockLogger.Object);

        var result = await service.AggregateAsync();

        result.TotalFetched.Should().Be(5);
        result.TotalUpserted.Should().Be(5);
        result.SourceResults.Should().HaveCount(3);
        result.SourceResults.Count(r => r.Success).Should().Be(2);
        result.SourceResults.Count(r => !r.Success).Should().Be(1);

        var failedSource = result.SourceResults.First(r => r.SourceName == "SourceB");
        failedSource.Success.Should().BeFalse();
        failedSource.ErrorMessage.Should().Be("Connection timeout");
        failedSource.Count.Should().Be(0);
    }

    [Fact]
    public async Task AggregateAsync_WithAllSourcesFailing_ReturnsZeroFetched()
    {
        var mockSource1 = CreateFailingMockSource("SourceA", "Error A");
        var mockSource2 = CreateFailingMockSource("SourceB", "Error B");
        var mockSource3 = CreateFailingMockSource("SourceC", "Error C");

        var sources = new[] { mockSource1.Object, mockSource2.Object, mockSource3.Object };
        var service = new AggregationService(sources, _mockRepository.Object, _mockCategorization.Object, _mockLogger.Object);

        var result = await service.AggregateAsync();

        result.TotalFetched.Should().Be(0);
        result.TotalUpserted.Should().Be(0);
        result.SourceResults.Should().HaveCount(3);
        result.SourceResults.Should().AllSatisfy(r => r.Success.Should().BeFalse());
        result.SourceResults.All(r => r.ErrorMessage != null).Should().BeTrue();
    }

    [Fact]
    public async Task AggregateAsync_CategorizesUncategorizedTransactions()
    {
        var transaction = new UnifiedTransaction
        {
            Id = Guid.NewGuid(),
            ExternalId = "T001",
            Category = TransactionCategory.Uncategorized,
            Description = "Test description",
            MerchantName = "Test merchant",
            Amount = 100m
        };

        var mockSource = new Mock<ITransactionSource>();
        mockSource.Setup(s => s.SourceName).Returns("TestSource");
        mockSource.Setup(s => s.FetchTransactionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UnifiedTransaction> { transaction });

        _mockCategorization.Setup(c => c.Categorize(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()))
            .Returns(TransactionCategory.Food);

        var service = new AggregationService(new[] { mockSource.Object }, _mockRepository.Object, _mockCategorization.Object, _mockLogger.Object);

        await service.AggregateAsync();

        _mockCategorization.Verify(c => c.Categorize("Test description", "Test merchant", 100m), Times.Once);
    }

    [Fact]
    public async Task AggregateAsync_DoesNotRecategorizeAlreadyCategorizedTransactions()
    {
        var transaction = new UnifiedTransaction
        {
            Id = Guid.NewGuid(),
            ExternalId = "T001",
            Category = TransactionCategory.Food,
            Description = "Test description",
            MerchantName = "Test merchant",
            Amount = 100m
        };

        var mockSource = new Mock<ITransactionSource>();
        mockSource.Setup(s => s.SourceName).Returns("TestSource");
        mockSource.Setup(s => s.FetchTransactionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UnifiedTransaction> { transaction });

        var service = new AggregationService(new[] { mockSource.Object }, _mockRepository.Object, _mockCategorization.Object, _mockLogger.Object);

        await service.AggregateAsync();

        _mockCategorization.Verify(c => c.Categorize(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()), Times.Never);
    }

    [Fact]
    public async Task AggregateAsync_CallsUpsertRangeOnRepository()
    {
        var mockSource = CreateMockSource("SourceA", 3);
        var service = new AggregationService(new[] { mockSource.Object }, _mockRepository.Object, _mockCategorization.Object, _mockLogger.Object);

        await service.AggregateAsync();

        _mockRepository.Verify(r => r.UpsertRangeAsync(It.IsAny<IEnumerable<UnifiedTransaction>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AggregateAsync_DoesNotCallUpsertWhenNoTransactionsFetched()
    {
        var mockSource = CreateMockSource("SourceA", 0);
        var service = new AggregationService(new[] { mockSource.Object }, _mockRepository.Object, _mockCategorization.Object, _mockLogger.Object);

        await service.AggregateAsync();

        _mockRepository.Verify(r => r.UpsertRangeAsync(It.IsAny<IEnumerable<UnifiedTransaction>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AggregateAsync_PopulatesDuration()
    {
        var mockSource = CreateMockSource("SourceA", 2);
        var service = new AggregationService(new[] { mockSource.Object }, _mockRepository.Object, _mockCategorization.Object, _mockLogger.Object);

        var result = await service.AggregateAsync();

        result.SourceResults.Should().HaveCount(1);
        result.SourceResults[0].Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task AggregateAsync_IncludesErrorMessageOnFailure()
    {
        var mockSource = CreateFailingMockSource("SourceA", "Network error occurred");
        var service = new AggregationService(new[] { mockSource.Object }, _mockRepository.Object, _mockCategorization.Object, _mockLogger.Object);

        var result = await service.AggregateAsync();

        result.SourceResults[0].ErrorMessage.Should().Be("Network error occurred");
    }

    [Fact]
    public async Task AggregateAsync_ReturnsCorrectSourceNames()
    {
        var mockSource1 = CreateMockSource("SourceA", 1);
        var mockSource2 = CreateMockSource("SourceB", 1);
        var mockSource3 = CreateMockSource("SourceC", 1);

        var sources = new[] { mockSource1.Object, mockSource2.Object, mockSource3.Object };
        var service = new AggregationService(sources, _mockRepository.Object, _mockCategorization.Object, _mockLogger.Object);

        var result = await service.AggregateAsync();

        result.SourceResults.Select(r => r.SourceName).Should().BeEquivalentTo(new[] { "SourceA", "SourceB", "SourceC" });
    }

    [Fact]
    public async Task AggregateAsync_ReturnsCorrectCountPerSource()
    {
        var mockSource1 = CreateMockSource("SourceA", 10);
        var mockSource2 = CreateMockSource("SourceB", 5);
        var mockSource3 = CreateMockSource("SourceC", 3);

        var sources = new[] { mockSource1.Object, mockSource2.Object, mockSource3.Object };
        var service = new AggregationService(sources, _mockRepository.Object, _mockCategorization.Object, _mockLogger.Object);

        var result = await service.AggregateAsync();

        var sourceA = result.SourceResults.First(r => r.SourceName == "SourceA");
        var sourceB = result.SourceResults.First(r => r.SourceName == "SourceB");
        var sourceC = result.SourceResults.First(r => r.SourceName == "SourceC");

        sourceA.Count.Should().Be(10);
        sourceB.Count.Should().Be(5);
        sourceC.Count.Should().Be(3);
    }

    [Fact]
    public async Task AggregateAsync_HandlesEmptySourceList()
    {
        var sources = Array.Empty<ITransactionSource>();
        var service = new AggregationService(sources, _mockRepository.Object, _mockCategorization.Object, _mockLogger.Object);

        var result = await service.AggregateAsync();

        result.TotalFetched.Should().Be(0);
        result.TotalUpserted.Should().Be(0);
        result.SourceResults.Should().BeEmpty();
    }

    [Fact]
    public async Task AggregateAsync_CategorizesSetsCorrectCategory()
    {
        var transaction = new UnifiedTransaction
        {
            Id = Guid.NewGuid(),
            ExternalId = "T001",
            Category = TransactionCategory.Uncategorized,
            Description = "Checkers",
            MerchantName = "Checkers",
            Amount = 250m
        };

        var mockSource = new Mock<ITransactionSource>();
        mockSource.Setup(s => s.SourceName).Returns("TestSource");
        mockSource.Setup(s => s.FetchTransactionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UnifiedTransaction> { transaction });

        _mockCategorization.Setup(c => c.Categorize("Checkers", "Checkers", 250m))
            .Returns(TransactionCategory.Food);

        var service = new AggregationService(new[] { mockSource.Object }, _mockRepository.Object, _mockCategorization.Object, _mockLogger.Object);

        await service.AggregateAsync();

        transaction.Category.Should().Be(TransactionCategory.Food);
    }

    [Fact]
    public async Task AggregateAsync_PassesCancellationToken()
    {
        var cts = new CancellationTokenSource();
        var mockSource = CreateMockSource("SourceA", 2);
        var service = new AggregationService(new[] { mockSource.Object }, _mockRepository.Object, _mockCategorization.Object, _mockLogger.Object);

        await service.AggregateAsync(cts.Token);

        mockSource.Verify(s => s.FetchTransactionsAsync(cts.Token), Times.Once);
    }

    private Mock<ITransactionSource> CreateMockSource(string sourceName, int transactionCount)
    {
        var transactions = Enumerable.Range(1, transactionCount)
            .Select(i => new UnifiedTransaction
            {
                Id = Guid.NewGuid(),
                ExternalId = $"{sourceName}_{i}",
                SourceSystem = sourceName,
                CustomerId = $"C{i:000}",
                AccountId = $"ACC{i:000}",
                Amount = 100m * i,
                Category = TransactionCategory.Food,
                Description = $"Transaction {i}",
                TransactionDateUtc = DateTime.UtcNow
            })
            .ToList();

        var mockSource = new Mock<ITransactionSource>();
        mockSource.Setup(s => s.SourceName).Returns(sourceName);
        mockSource.Setup(s => s.FetchTransactionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        return mockSource;
    }

    private Mock<ITransactionSource> CreateFailingMockSource(string sourceName, string errorMessage)
    {
        var mockSource = new Mock<ITransactionSource>();
        mockSource.Setup(s => s.SourceName).Returns(sourceName);
        mockSource.Setup(s => s.FetchTransactionsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(errorMessage));

        return mockSource;
    }
}
