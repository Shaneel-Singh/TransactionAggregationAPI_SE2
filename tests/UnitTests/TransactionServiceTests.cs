using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TransactionAggregationAPI.Application.Domain;
using TransactionAggregationAPI.Application.Interfaces;
using TransactionAggregationAPI.Application.Services;
using Xunit;

namespace TransactionAggregationAPI.UnitTests;

public class TransactionServiceTests
{
    private readonly Mock<ITransactionRepository> _mockRepository;
    private readonly Mock<ITransactionCacheService> _mockCache;
    private readonly Mock<ICategorizationService> _mockCategorization;
    private readonly Mock<ILogger<TransactionService>> _mockLogger;
    private readonly TransactionService _service;

    public TransactionServiceTests()
    {
        _mockRepository = new Mock<ITransactionRepository>();
        _mockCache = new Mock<ITransactionCacheService>();
        _mockCategorization = new Mock<ICategorizationService>();
        _mockLogger = new Mock<ILogger<TransactionService>>();
        _service = new TransactionService(_mockRepository.Object, _mockCache.Object, _mockCategorization.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetByIdAsync_CacheHit_ReturnsCachedTransaction_RepositoryNotCalled()
    {
        var transactionId = Guid.NewGuid();
        var cachedTransaction = new UnifiedTransaction
        {
            Id = transactionId,
            CustomerId = "C001",
            Description = "Cached transaction"
        };

        _mockCache.Setup(c => c.GetTransactionAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedTransaction);

        var result = await _service.GetByIdAsync(transactionId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(transactionId);
        result.Description.Should().Be("Cached transaction");
        _mockRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_CacheMiss_FallsThroughToRepository_ThenCachesResult()
    {
        var transactionId = Guid.NewGuid();
        var repoTransaction = new UnifiedTransaction
        {
            Id = transactionId,
            CustomerId = "C001",
            Description = "Repo transaction"
        };

        _mockCache.Setup(c => c.GetTransactionAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UnifiedTransaction?)null);
        _mockRepository.Setup(r => r.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(repoTransaction);

        var result = await _service.GetByIdAsync(transactionId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(transactionId);
        _mockRepository.Verify(r => r.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(c => c.SetTransactionAsync(repoTransaction, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NotFoundInCacheOrRepo_ReturnsNull()
    {
        var transactionId = Guid.NewGuid();

        _mockCache.Setup(c => c.GetTransactionAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UnifiedTransaction?)null);
        _mockRepository.Setup(r => r.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UnifiedTransaction?)null);

        var result = await _service.GetByIdAsync(transactionId);

        result.Should().BeNull();
        _mockCache.Verify(c => c.SetTransactionAsync(It.IsAny<UnifiedTransaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_CategorizesTransaction_SavesToRepo_InvalidatesCaches()
    {
        var createdTransaction = new UnifiedTransaction
        {
            Id = Guid.NewGuid(),
            CustomerId = "C001",
            AccountId = "ACC001",
            Amount = 100m,
            Description = "Test transaction",
            MerchantName = "Test merchant",
            Category = TransactionCategory.Food
        };

        _mockCategorization.Setup(c => c.Categorize(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()))
            .Returns(TransactionCategory.Food);
        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<UnifiedTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdTransaction);

        var result = await _service.CreateAsync("C001", "ACC001", 100m, "ZAR", "Test transaction", "Test merchant", "debit", DateTime.UtcNow, "user1");

        result.Should().NotBeNull();
        _mockCategorization.Verify(c => c.Categorize("Test transaction", "Test merchant", 100m), Times.Once);
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<UnifiedTransaction>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(c => c.InvalidateCustomerListAsync("C001", It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(c => c.InvalidateSummaryAsync("C001", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ReturnsFalse()
    {
        var transactionId = Guid.NewGuid();

        _mockRepository.Setup(r => r.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UnifiedTransaction?)null);

        var result = await _service.DeleteAsync(transactionId, "user1");

        result.Should().BeFalse();
        _mockRepository.Verify(r => r.SoftDeleteAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_Found_SoftDeletes_InvalidatesCaches()
    {
        var transactionId = Guid.NewGuid();
        var existingTransaction = new UnifiedTransaction
        {
            Id = transactionId,
            CustomerId = "C001",
            Description = "Existing transaction"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTransaction);
        _mockRepository.Setup(r => r.SoftDeleteAsync(transactionId, "user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.DeleteAsync(transactionId, "user1");

        result.Should().BeTrue();
        _mockRepository.Verify(r => r.SoftDeleteAsync(transactionId, "user1", It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(c => c.InvalidateTransactionAsync(transactionId, It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(c => c.InvalidateCustomerListAsync("C001", It.IsAny<CancellationToken>()), Times.Once);
        _mockCache.Verify(c => c.InvalidateSummaryAsync("C001", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByCustomerAsync_PageSizeCappedAt100()
    {
        _mockRepository.Setup(r => r.GetByCustomerIdAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<UnifiedTransaction>(), 0));

        await _service.GetByCustomerAsync("C001", 1, 150, null, null, null, null, null);

        _mockRepository.Verify(r => r.GetByCustomerIdAsync(
            "C001",
            1,
            100,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByCustomerAsync_PageSizeNotCappedWhenBelow100()
    {
        _mockRepository.Setup(r => r.GetByCustomerIdAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<UnifiedTransaction>(), 0));

        await _service.GetByCustomerAsync("C001", 1, 50, null, null, null, null, null);

        _mockRepository.Verify(r => r.GetByCustomerIdAsync(
            "C001",
            1,
            50,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_PageSizeCappedAt100()
    {
        _mockRepository.Setup(r => r.GetAllAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<UnifiedTransaction>(), 0));

        await _service.GetAllAsync(1, 200, null, null, null, null, null);

        _mockRepository.Verify(r => r.GetAllAsync(
            1,
            100,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_PageSizeNotCappedWhenBelow100()
    {
        _mockRepository.Setup(r => r.GetAllAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<UnifiedTransaction>(), 0));

        await _service.GetAllAsync(1, 25, null, null, null, null, null);

        _mockRepository.Verify(r => r.GetAllAsync(
            1,
            25,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_SetsManualSourceSystem()
    {
        UnifiedTransaction? capturedTransaction = null;
        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<UnifiedTransaction>(), It.IsAny<CancellationToken>()))
            .Callback<UnifiedTransaction, CancellationToken>((tx, ct) => capturedTransaction = tx)
            .ReturnsAsync((UnifiedTransaction tx, CancellationToken ct) => tx);

        _mockCategorization.Setup(c => c.Categorize(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()))
            .Returns(TransactionCategory.Food);

        await _service.CreateAsync("C001", "ACC001", 100m, "ZAR", "Test", "Merchant", "debit", DateTime.UtcNow, "user1");

        capturedTransaction.Should().NotBeNull();
        capturedTransaction!.SourceSystem.Should().Be("manual");
    }

    [Fact]
    public async Task CreateAsync_SetsCreatedByAndUpdatedBy()
    {
        UnifiedTransaction? capturedTransaction = null;
        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<UnifiedTransaction>(), It.IsAny<CancellationToken>()))
            .Callback<UnifiedTransaction, CancellationToken>((tx, ct) => capturedTransaction = tx)
            .ReturnsAsync((UnifiedTransaction tx, CancellationToken ct) => tx);

        _mockCategorization.Setup(c => c.Categorize(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()))
            .Returns(TransactionCategory.Food);

        await _service.CreateAsync("C001", "ACC001", 100m, "ZAR", "Test", "Merchant", "debit", DateTime.UtcNow, "admin");

        capturedTransaction.Should().NotBeNull();
        capturedTransaction!.CreatedBy.Should().Be("admin");
        capturedTransaction.UpdatedBy.Should().Be("admin");
    }

    [Fact]
    public async Task GetSummaryAsync_CallsRepository()
    {
        var summary = new TransactionSummary(
            "C001",
            10,
            1000m,
            100m,
            500m,
            50m,
            new Dictionary<TransactionCategory, int>(),
            new Dictionary<TransactionCategory, decimal>(),
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow);

        _mockRepository.Setup(r => r.GetSummaryAsync("C001", It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        var result = await _service.GetSummaryAsync("C001", null, null);

        result.Should().NotBeNull();
        result.CustomerId.Should().Be("C001");
        result.TotalCount.Should().Be(10);
        _mockRepository.Verify(r => r.GetSummaryAsync("C001", null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_GeneratesNewGuid()
    {
        UnifiedTransaction? capturedTransaction = null;
        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<UnifiedTransaction>(), It.IsAny<CancellationToken>()))
            .Callback<UnifiedTransaction, CancellationToken>((tx, ct) => capturedTransaction = tx)
            .ReturnsAsync((UnifiedTransaction tx, CancellationToken ct) => tx);

        _mockCategorization.Setup(c => c.Categorize(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()))
            .Returns(TransactionCategory.Food);

        await _service.CreateAsync("C001", "ACC001", 100m, "ZAR", "Test", "Merchant", "debit", DateTime.UtcNow, "user1");

        capturedTransaction.Should().NotBeNull();
        capturedTransaction!.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task DeleteAsync_DoesNotInvalidateCachesWhenSoftDeleteFails()
    {
        var transactionId = Guid.NewGuid();
        var existingTransaction = new UnifiedTransaction
        {
            Id = transactionId,
            CustomerId = "C001"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTransaction);
        _mockRepository.Setup(r => r.SoftDeleteAsync(transactionId, "user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _service.DeleteAsync(transactionId, "user1");

        result.Should().BeFalse();
        _mockCache.Verify(c => c.InvalidateTransactionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockCache.Verify(c => c.InvalidateCustomerListAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockCache.Verify(c => c.InvalidateSummaryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
