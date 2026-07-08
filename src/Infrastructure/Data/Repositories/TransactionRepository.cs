using Microsoft.EntityFrameworkCore;
using TransactionAggregationAPI.Application.Domain;
using TransactionAggregationAPI.Application.Interfaces;

namespace TransactionAggregationAPI.Infrastructure.Data.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly TransactionDbContext _db;

    public TransactionRepository(TransactionDbContext db)
    {
        _db = db;
    }

    public async Task<UnifiedTransaction?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Transactions.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<UnifiedTransaction?> GetByExternalIdAndSourceAsync(string externalId, string sourceSystem, CancellationToken ct = default)
        => await _db.Transactions.AsNoTracking()
            .FirstOrDefaultAsync(t => t.ExternalId == externalId && t.SourceSystem == sourceSystem, ct);

    public async Task<(IReadOnlyList<UnifiedTransaction> Items, int TotalCount)> GetByCustomerIdAsync(
        string customerId, int page, int pageSize, string? sortBy, string? sortOrder,
        string? category, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var query = _db.Transactions.AsNoTracking().Where(t => t.CustomerId == customerId);
        query = ApplyFilters(query, category, from, to);
        var total = await query.CountAsync(ct);
        query = ApplySorting(query, sortBy, sortOrder);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task<(IReadOnlyList<UnifiedTransaction> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize, string? sortBy, string? sortOrder,
        string? category, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var query = _db.Transactions.AsNoTracking();
        query = ApplyFilters(query, category, from, to);
        var total = await query.CountAsync(ct);
        query = ApplySorting(query, sortBy, sortOrder);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task<UnifiedTransaction> CreateAsync(UnifiedTransaction transaction, CancellationToken ct = default)
    {
        transaction.CreatedAtUtc = DateTime.UtcNow;
        transaction.UpdatedAtUtc = DateTime.UtcNow;
        await _db.Transactions.AddAsync(transaction, ct);
        await _db.SaveChangesAsync(ct);
        return transaction;
    }

    public async Task<UnifiedTransaction> UpdateAsync(UnifiedTransaction transaction, CancellationToken ct = default)
    {
        transaction.UpdatedAtUtc = DateTime.UtcNow;
        _db.Transactions.Update(transaction);
        await _db.SaveChangesAsync(ct);
        return transaction;
    }

    public async Task<bool> SoftDeleteAsync(Guid id, string deletedBy, CancellationToken ct = default)
    {
        var tx = await _db.Transactions.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tx is null) return false;
        tx.IsDeleted = true;
        tx.DeletedAtUtc = DateTime.UtcNow;
        tx.DeletedBy = deletedBy;
        tx.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task UpsertRangeAsync(IEnumerable<UnifiedTransaction> transactions, CancellationToken ct = default)
    {
        var txList = transactions.ToList();
        if (txList.Count == 0) return;

        foreach (var tx in txList)
        {
            var existing = await _db.Transactions
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.ExternalId == tx.ExternalId && t.SourceSystem == tx.SourceSystem, ct);

            if (existing is null)
            {
                tx.Id = Guid.NewGuid();
                tx.CreatedAtUtc = DateTime.UtcNow;
                tx.UpdatedAtUtc = DateTime.UtcNow;
                await _db.Transactions.AddAsync(tx, ct);
            }
            else
            {
                existing.Amount = tx.Amount;
                existing.Description = tx.Description;
                existing.MerchantName = tx.MerchantName;
                existing.Category = tx.Category;
                existing.TransactionDateUtc = tx.TransactionDateUtc;
                existing.Currency = tx.Currency;
                existing.UpdatedAtUtc = DateTime.UtcNow;
                existing.UpdatedBy = "aggregation";
                existing.RawPayload = tx.RawPayload;
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<Dictionary<TransactionCategory, decimal>> GetCategoryTotalsAsync(
        string customerId, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var query = _db.Transactions.AsNoTracking().Where(t => t.CustomerId == customerId);
        if (from.HasValue) query = query.Where(t => t.TransactionDateUtc >= from.Value);
        if (to.HasValue) query = query.Where(t => t.TransactionDateUtc <= to.Value);

        return await query.GroupBy(t => t.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(t => t.Amount) })
            .ToDictionaryAsync(x => x.Category, x => x.Total, ct);
    }

    public async Task<TransactionSummary> GetSummaryAsync(
        string customerId, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var query = _db.Transactions.AsNoTracking().Where(t => t.CustomerId == customerId);
        if (from.HasValue) query = query.Where(t => t.TransactionDateUtc >= from.Value);
        if (to.HasValue) query = query.Where(t => t.TransactionDateUtc <= to.Value);

        var transactions = await query.ToListAsync(ct);
        if (transactions.Count == 0)
            return new TransactionSummary(customerId, 0, 0, 0, 0, 0,
                new Dictionary<TransactionCategory, int>(),
                new Dictionary<TransactionCategory, decimal>(),
                null, null);

        return new TransactionSummary(
            customerId,
            transactions.Count,
            transactions.Sum(t => t.Amount),
            transactions.Average(t => t.Amount),
            transactions.Max(t => t.Amount),
            transactions.Min(t => t.Amount),
            transactions.GroupBy(t => t.Category).ToDictionary(g => g.Key, g => g.Count()),
            transactions.GroupBy(t => t.Category).ToDictionary(g => g.Key, g => g.Sum(t => t.Amount)),
            transactions.Min(t => (DateTime?)t.TransactionDateUtc),
            transactions.Max(t => (DateTime?)t.TransactionDateUtc));
    }

    private static IQueryable<UnifiedTransaction> ApplyFilters(
        IQueryable<UnifiedTransaction> query, string? category, DateTime? from, DateTime? to)
    {
        if (!string.IsNullOrWhiteSpace(category) && Enum.TryParse<TransactionCategory>(category, true, out var cat))
            query = query.Where(t => t.Category == cat);
        if (from.HasValue) query = query.Where(t => t.TransactionDateUtc >= from.Value);
        if (to.HasValue) query = query.Where(t => t.TransactionDateUtc <= to.Value);
        return query;
    }

    private static IQueryable<UnifiedTransaction> ApplySorting(
        IQueryable<UnifiedTransaction> query, string? sortBy, string? sortOrder)
    {
        var desc = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
        return sortBy?.ToLowerInvariant() switch
        {
            "amount" => desc ? query.OrderByDescending(t => t.Amount) : query.OrderBy(t => t.Amount),
            "date" or "transactiondate" => desc ? query.OrderByDescending(t => t.TransactionDateUtc) : query.OrderBy(t => t.TransactionDateUtc),
            "category" => desc ? query.OrderByDescending(t => t.Category) : query.OrderBy(t => t.Category),
            "merchant" => desc ? query.OrderByDescending(t => t.MerchantName) : query.OrderBy(t => t.MerchantName),
            _ => query.OrderByDescending(t => t.TransactionDateUtc)
        };
    }
}
