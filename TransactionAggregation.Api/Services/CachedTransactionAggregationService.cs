using System.Diagnostics;
using System.Diagnostics.Metrics;
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
    private static readonly string AssemblyVersion = typeof(CachedTransactionAggregationService)
        .Assembly
        .GetName()
        .Version?
        .ToString() ?? "1.0.0";
    private const string MeterName = "TransactionAggregation.Api.Caching";
    private const string HitCounterName = "transaction_cache_hits";
    private const string MissCounterName = "transaction_cache_misses";
    private static readonly Meter CacheMeter = new(MeterName, AssemblyVersion);
    private static readonly Counter<long> CacheHitCounter = CacheMeter.CreateCounter<long>(
        HitCounterName,
        description: "Total cache hits for transaction aggregation requests.");
    private static readonly Counter<long> CacheMissCounter = CacheMeter.CreateCounter<long>(
        MissCounterName,
        description: "Total cache misses for transaction aggregation requests.");

    private readonly ITransactionAggregationService _inner;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedTransactionAggregationService> _logger;
    private readonly Counter<long> _hitCounter;
    private readonly Counter<long> _missCounter;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachedTransactionAggregationService"/> class.
    /// </summary>
    /// <param name="inner">The inner transaction aggregation service.</param>
    /// <param name="cache">The memory cache.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="meterFactory">Optional meter factory for advanced telemetry funnels.</param>
    public CachedTransactionAggregationService(
        ITransactionAggregationService inner,
        IMemoryCache cache,
        ILogger<CachedTransactionAggregationService> logger,
        IMeterFactory? meterFactory = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (meterFactory is null)
        {
            _hitCounter = CacheHitCounter;
            _missCounter = CacheMissCounter;
            return;
        }

        var meter = meterFactory.Create(new MeterOptions(MeterName)
        {
            Version = AssemblyVersion
        });
        _hitCounter = meter.CreateCounter<long>(HitCounterName, description: "Total cache hits for transaction aggregation requests.");
        _missCounter = meter.CreateCounter<long>(MissCounterName, description: "Total cache misses for transaction aggregation requests.");
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

        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<T>? cached) && cached is not null)
        {
            RecordCacheHit(cachePrefix, customerId);
            _logger.LogInformation("Cache hit for {CachePrefix} {CustomerId} ({CacheKey})", cachePrefix, customerId, cacheKey);
            return Task.FromResult(cached);
        }

        RecordCacheMiss(cachePrefix, customerId);
        _logger.LogInformation("Cache miss for {CachePrefix} {CustomerId} ({CacheKey})", cachePrefix, customerId, cacheKey);
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
        _logger.LogInformation("Cached {Count} items for {CustomerId} under {CacheKey}", snapshot.Count, customerId, cacheKey);

        return snapshot;
    }

    private static bool ShouldCache(string customerId)
        => !string.IsNullOrWhiteSpace(customerId);

    private void RecordCacheHit(string cachePrefix, string customerId)
    {
        var tags = BuildTags(cachePrefix, customerId);
        _hitCounter.Add(1, tags);
    }

    private void RecordCacheMiss(string cachePrefix, string customerId)
    {
        var tags = BuildTags(cachePrefix, customerId);
        _missCounter.Add(1, tags);
    }

    private static TagList BuildTags(string cachePrefix, string customerId)
    {
        var tags = new TagList
        {
            { "cache_prefix", cachePrefix },
            { "customer_id", customerId }
        };
        return tags;
    }

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
