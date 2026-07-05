using Microsoft.Extensions.Logging;
using TransactionAggregationAPI.Application.Domain;
using TransactionAggregationAPI.Application.Interfaces;

namespace TransactionAggregationAPI.Application.Services;

public class TransactionService
{
    private readonly ITransactionRepository _repository;
    private readonly ITransactionCacheService _cache;
    private readonly ICategorizationService _categorization;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(
        ITransactionRepository repository,
        ITransactionCacheService cache,
        ICategorizationService categorization,
        ILogger<TransactionService> logger)
    {
        _repository = repository;
        _cache = cache;
        _categorization = categorization;
        _logger = logger;
    }

    public async Task<UnifiedTransaction?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var cached = await _cache.GetTransactionAsync(id, ct);
        if (cached is not null)
        {
            _logger.LogDebug("Cache hit for transaction {Id}", id);
            return cached;
        }

        var transaction = await _repository.GetByIdAsync(id, ct);
        if (transaction is not null)
            await _cache.SetTransactionAsync(transaction, ct);

        return transaction;
    }

    public async Task<(IReadOnlyList<UnifiedTransaction> Items, int TotalCount)> GetByCustomerAsync(
        string customerId,
        int page,
        int pageSize,
        string? sortBy,
        string? sortOrder,
        string? category,
        DateTime? from,
        DateTime? to,
        CancellationToken ct = default)
    {
        pageSize = Math.Min(pageSize, 100);
        return await _repository.GetByCustomerIdAsync(customerId, page, pageSize, sortBy, sortOrder, category, from, to, ct);
    }

    public async Task<(IReadOnlyList<UnifiedTransaction> Items, int TotalCount)> GetAllAsync(
        int page,
        int pageSize,
        string? sortBy,
        string? sortOrder,
        string? category,
        DateTime? from,
        DateTime? to,
        CancellationToken ct = default)
    {
        pageSize = Math.Min(pageSize, 100);
        return await _repository.GetAllAsync(page, pageSize, sortBy, sortOrder, category, from, to, ct);
    }

    public async Task<UnifiedTransaction> CreateAsync(
        string customerId,
        string accountId,
        decimal amount,
        string currency,
        string description,
        string merchantName,
        string transactionType,
        DateTime transactionDateUtc,
        string createdBy,
        CancellationToken ct = default)
    {
        var category = _categorization.Categorize(description, merchantName, amount);
        var transaction = new UnifiedTransaction
        {
            Id = Guid.NewGuid(),
            ExternalId = Guid.NewGuid().ToString(),
            SourceSystem = "manual",
            CustomerId = customerId,
            AccountId = accountId,
            Amount = amount,
            Currency = currency,
            Description = description,
            MerchantName = merchantName,
            Category = category,
            TransactionType = transactionType,
            TransactionDateUtc = transactionDateUtc,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };

        var created = await _repository.CreateAsync(transaction, ct);
        await _cache.InvalidateCustomerListAsync(customerId, ct);
        await _cache.InvalidateSummaryAsync(customerId, ct);

        _logger.LogInformation("Created transaction {Id} for customer {CustomerId}", created.Id, customerId);
        return created;
    }

    public async Task<bool> DeleteAsync(Guid id, string deletedBy, CancellationToken ct = default)
    {
        var existing = await _repository.GetByIdAsync(id, ct);
        if (existing is null) return false;

        var result = await _repository.SoftDeleteAsync(id, deletedBy, ct);
        if (result)
        {
            await _cache.InvalidateTransactionAsync(id, ct);
            await _cache.InvalidateCustomerListAsync(existing.CustomerId, ct);
            await _cache.InvalidateSummaryAsync(existing.CustomerId, ct);
        }
        return result;
    }

    public async Task<TransactionSummary> GetSummaryAsync(
        string customerId,
        DateTime? from,
        DateTime? to,
        CancellationToken ct = default)
    {
        return await _repository.GetSummaryAsync(customerId, from, to, ct);
    }
}
