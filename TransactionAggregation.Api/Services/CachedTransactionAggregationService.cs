using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TransactionAggregation.Domain.Abstractions;
using TransactionAggregation.Domain.Models;

namespace TransactionAggregation.Api.Services;

/// <summary>
/// Decorates <see cref="ITransactionAggregationService"/> with an IMemoryCache-backed layer.
/// Ensures repeated reads for the same customer and date range reuse the same materialized payload.
/// </summary>
public sealed class CachedTransactionAggregationService : ITransactionAggregationService
{
    private static readonly TimeSpan AbsoluteTtl = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan SlidingTtl = TimeSpan.FromMinutes(1);

    private readonly ITransactionAggregationService _inner;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedTransactionAggregationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachedTransactionAggregationService"/> class.
    /// </summary>
    /// <param name="inner">The inner transaction aggregation service.</param>
    /// <param name="cache">The memory cache.</param>
    /// <param name="logger">The logger.</param>
    public CachedTransactionAggregationService(
        ITransactionAggregationService inner,
        IMemoryCache cache,
        ILogger<CachedTransactionAggregationService> logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<UnifiedTransaction>> GetAllAsync(
        string customerId,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
    {
        return ExecuteWithCache(
            cachePrefix: "txns",
            customerId,
            from,
            to,
            ct,
            () => _inner.GetAllAsync(customerId, from, to, ct));
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<CategorySummary>> GetCategorySummaryAsync(
        string customerId,
        DateTime? from,
        DateTime? to,
        CancellationToken ct)
    {
        return ExecuteWithCache(
            cachePrefix: "summary",
            customerId,
            from,
            to,
            ct,
            () => _inner.GetCategorySummaryAsync(customerId, from, to, ct));
    }

    private Task<IReadOnlyList<T>> ExecuteWithCache<T>(
        string cachePrefix,
        string customerId,
        DateTime? from,
        DateTime? to,
        CancellationToken ct,
        Func<Task<IReadOnlyList<T>>> factory)
    {
        if (!ShouldCache(customerId))
            return factory();

        ct.ThrowIfCancellationRequested();

        var cacheKey = BuildCacheKey(cachePrefix, customerId, from, to);

        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<T>? cached))
            return Task.FromResult(cached);

        return FetchAndCache(cacheKey, factory, customerId);
    }

    private async Task<IReadOnlyList<T>> FetchAndCache<T>(
        string cacheKey,
        Func<Task<IReadOnlyList<T>>> factory,
        string customerId)
    {
        var fresh = await factory().ConfigureAwait(false);
        var snapshot = Materialize(fresh);

        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = AbsoluteTtl,
            SlidingExpiration = SlidingTtl,
            Priority = CacheItemPriority.Normal
        };

        _cache.Set(cacheKey, snapshot, options);
        _logger.LogDebug("Cached {Count} items for {CustomerId} under {CacheKey}", snapshot.Count, customerId, cacheKey);

        return snapshot;
    }

    private static bool ShouldCache(string customerId)
        => !string.IsNullOrWhiteSpace(customerId);

    private static string BuildCacheKey(string prefix, string customerId, DateTime? from, DateTime? to)
    {
        var fromTicks = from?.Ticks.ToString(CultureInfo.InvariantCulture) ?? "none";
        var toTicks = to?.Ticks.ToString(CultureInfo.InvariantCulture) ?? "none";
        return $"{prefix}:{customerId}:{fromTicks}:{toTicks}";
    }

    private static IReadOnlyList<T> Materialize<T>(IReadOnlyList<T> source)
    {
        if (source.Count == 0)
            return Array.Empty<T>();

        return source switch
        {
            T[] array => array,
            List<T> list => list.ToArray(),
            _ => source.ToArray()
        };
    }
}
