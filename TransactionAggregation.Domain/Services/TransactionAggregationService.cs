using TransactionAggregation.Domain.Abstractions;
using TransactionAggregation.Domain.Models;

namespace TransactionAggregation.Domain.Services;

/// <summary>
/// Orchestrates calls to all registered source adapters and
/// returns a unified, sorted transaction list.
/// </summary>
public sealed class TransactionAggregationService : ITransactionAggregationService
{
    private readonly IEnumerable<ITransactionSourceAdapter> _sources;

    public TransactionAggregationService(IEnumerable<ITransactionSourceAdapter> sources)
    {
        _sources = sources;
    }

    public async Task<IReadOnlyList<UnifiedTransaction>> GetAllAsync(
        string customerId,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
    {
        // Fire all sources in parallel
        var tasks = _sources.Select(s => s.FetchAndNormalizeAsync(customerId, ct));
        var results = await Task.WhenAll(tasks);

        var all = results.SelectMany(x => x);

        if (from.HasValue)
            all = all.Where(t => t.Timestamp >= from.Value);

        if (to.HasValue)
            all = all.Where(t => t.Timestamp <= to.Value);

        // Sort newest first
        return all
            .OrderByDescending(t => t.Timestamp)
            .ToList();
    }

    public async Task<IReadOnlyList<CategorySummary>> GetCategorySummaryAsync(
        string customerId,
        DateTime? from,
        DateTime? to,
        CancellationToken ct)
    {
        var txns = await GetAllAsync(customerId, from, to, ct);

        // return empty list early
        if (txns.Count == 0)
            return Array.Empty<CategorySummary>();

        return txns
            .GroupBy(t => t.Category)
            .Select(g => new CategorySummary(
                Category: g.Key,
                TotalAmount: g.Sum(x => x.Amount),
                TransactionCount: g.Count()
            ))
            .OrderByDescending(s => Math.Abs(s.TotalAmount))
            .ToList();
    }

}