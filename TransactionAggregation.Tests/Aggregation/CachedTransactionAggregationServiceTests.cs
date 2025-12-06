using System.Diagnostics.Metrics;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using TransactionAggregation.Api.Services;
using TransactionAggregation.Domain.Abstractions;
using TransactionAggregation.Domain.Models;
using TransactionAggregation.Tests.TestUtils;
using Xunit;

namespace TransactionAggregation.Tests.Aggregation;

public class CachedTransactionAggregationServiceTests
{
    [Fact]
    public async Task GetAllAsync_Emits_Hit_And_Miss_Metrics_And_Logs()
    {
        // Arrange
        var customerId = "cust-001";
        var unifiedTransactions = new List<UnifiedTransaction>
        {
            new(
                TransactionId: "t-1",
                CustomerId: customerId,
                Amount: 10,
                Currency: "ZAR",
                Timestamp: DateTime.UtcNow,
                Description: "Coffee",
                Category: "Food",
                SourceName: "ASource")
        };

        var inner = new Mock<ITransactionAggregationService>();
        inner.Setup(s => s.GetAllAsync(customerId, null, null, It.IsAny<CancellationToken>()))
             .ReturnsAsync(unifiedTransactions);

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new TestLogger<CachedTransactionAggregationService>();
        using var meterFactory = new RecordingMeterFactory();
        var measurements = new List<MeasurementRecord>();
        using var listener = CreateListener(measurements);

        var sut = new CachedTransactionAggregationService(inner.Object, cache, logger, meterFactory);

        // Act
        var firstResult = await sut.GetAllAsync(customerId);
        var secondResult = await sut.GetAllAsync(customerId);

        // Assert - cache behavior
        Assert.Same(firstResult, secondResult);
        inner.Verify(s => s.GetAllAsync(customerId, null, null, It.IsAny<CancellationToken>()), Times.Once);

        // Assert - metrics
        var miss = measurements.Single(m => m.InstrumentName == "transaction_cache_misses");
        Assert.Equal(1, miss.Value);
        Assert.Equal("txns", miss.Tags["cache_prefix"]);
        Assert.Equal(customerId, miss.Tags["customer_id"]);

        var hit = measurements.Single(m => m.InstrumentName == "transaction_cache_hits");
        Assert.Equal(1, hit.Value);
        Assert.Equal("txns", hit.Tags["cache_prefix"]);
        Assert.Equal(customerId, hit.Tags["customer_id"]);

        // Assert - logs
        Assert.Contains(logger.Entries, e => e.Message.Contains("Cache miss"));
        Assert.Contains(logger.Entries, e => e.Message.Contains("Cache hit"));
    }

    [Fact]
    public async Task GetCategorySummaryAsync_Shares_Cache_Instrumentation()
    {
        // Arrange
        var customerId = "cust-002";
        var summaries = new List<CategorySummary>
        {
            new("Groceries", 42m, 3)
        };

        var inner = new Mock<ITransactionAggregationService>();
        inner.Setup(s => s.GetCategorySummaryAsync(customerId, null, null, It.IsAny<CancellationToken>()))
             .ReturnsAsync(summaries);

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new TestLogger<CachedTransactionAggregationService>();
        using var meterFactory = new RecordingMeterFactory();
        var measurements = new List<MeasurementRecord>();
        using var listener = CreateListener(measurements);

        var sut = new CachedTransactionAggregationService(inner.Object, cache, logger, meterFactory);

        // Act
        await sut.GetCategorySummaryAsync(customerId, null, null, CancellationToken.None);
        await sut.GetCategorySummaryAsync(customerId, null, null, CancellationToken.None);

        // Assert - metrics
        Assert.Equal(1, measurements.Count(m => m.InstrumentName == "transaction_cache_misses"));
        Assert.Equal(1, measurements.Count(m => m.InstrumentName == "transaction_cache_hits"));

        // Assert - logs mention summary prefix
        Assert.Contains(logger.Entries, e => e.Message.Contains("Cache miss") && e.Message.Contains("summary"));
        Assert.Contains(logger.Entries, e => e.Message.Contains("Cache hit") && e.Message.Contains("summary"));
    }

    private static IReadOnlyDictionary<string, object?> Snapshot(ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var tag in tags)
        {
            dict[tag.Key] = tag.Value;
        }

        return dict;
    }

    private sealed record MeasurementRecord(string InstrumentName, long Value, IReadOnlyDictionary<string, object?> Tags);

    private const string CacheMeterName = "TransactionAggregation.Api.Caching";

    private static MeterListener CreateListener(List<MeasurementRecord> sink)
    {
        var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, instListener) =>
        {
            if (instrument.Meter.Name == CacheMeterName)
                instListener.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            if (instrument.Meter.Name != CacheMeterName)
                return;
            sink.Add(new MeasurementRecord(instrument.Name, measurement, Snapshot(tags)));
        });
        listener.Start();
        return listener;
    }

    private sealed class RecordingMeterFactory : IMeterFactory, IDisposable
    {
        private readonly List<Meter> _meters = new();

        public Meter Create(MeterOptions options)
        {
            var meter = new Meter(options.Name, options.Version);
            _meters.Add(meter);
            return meter;
        }

        public void Dispose()
        {
            foreach (var meter in _meters)
            {
                meter.Dispose();
            }
        }
    }
}
