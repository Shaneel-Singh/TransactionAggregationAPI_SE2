using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using TransactionAggregationAPI.Application.Domain;
using TransactionAggregationAPI.Application.Interfaces;

namespace TransactionAggregationAPI.Infrastructure.Cache;

public class RedisCacheService : ITransactionCacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;
    private long _hits;
    private long _misses;
    private long _invalidations;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static readonly DistributedCacheEntryOptions TransactionOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
    };
    private static readonly DistributedCacheEntryOptions CustomerListOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };
    private static readonly DistributedCacheEntryOptions SummaryOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
    };

    public RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<UnifiedTransaction?> GetTransactionAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var value = await _cache.GetStringAsync(TransactionKey(id), ct);
            if (value is null) { Interlocked.Increment(ref _misses); return null; }
            Interlocked.Increment(ref _hits);
            return JsonSerializer.Deserialize<UnifiedTransaction>(value, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis GET failed for transaction {Id}", id);
            return null;
        }
    }

    public async Task SetTransactionAsync(UnifiedTransaction transaction, CancellationToken ct = default)
    {
        try
        {
            var value = JsonSerializer.Serialize(transaction, JsonOptions);
            await _cache.SetStringAsync(TransactionKey(transaction.Id), value, TransactionOptions, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis SET failed for transaction {Id}", transaction.Id);
        }
    }

    public async Task InvalidateTransactionAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await _cache.RemoveAsync(TransactionKey(id), ct);
            Interlocked.Increment(ref _invalidations);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis REMOVE failed for transaction {Id}", id);
        }
    }

    public async Task InvalidateCustomerListAsync(string customerId, CancellationToken ct = default)
    {
        try
        {
            await _cache.RemoveAsync(CustomerListKey(customerId), ct);
            Interlocked.Increment(ref _invalidations);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis REMOVE failed for customer list {CustomerId}", customerId);
        }
    }

    public async Task InvalidateSummaryAsync(string customerId, CancellationToken ct = default)
    {
        try
        {
            await _cache.RemoveAsync(SummaryKey(customerId), ct);
            Interlocked.Increment(ref _invalidations);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis REMOVE failed for summary {CustomerId}", customerId);
        }
    }

    public Task<CacheStats> GetStatsAsync()
        => Task.FromResult(new CacheStats(
            Interlocked.Read(ref _hits),
            Interlocked.Read(ref _misses),
            Interlocked.Read(ref _invalidations)));

    private static string TransactionKey(Guid id) => $"tx:id:{id}";
    private static string CustomerListKey(string customerId) => $"tx:customer:{customerId}";
    private static string SummaryKey(string customerId) => $"tx:summary:{customerId}";
}
