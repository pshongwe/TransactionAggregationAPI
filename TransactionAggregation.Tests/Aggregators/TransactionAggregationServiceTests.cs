using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TransactionAggregation.Api.Adapters;
using TransactionAggregation.Domain.Abstractions;
using TransactionAggregation.Domain.Services;
using TransactionAggregation.Tests.TestUtils;
using Xunit;

namespace TransactionAggregation.Tests.Aggregators;

public class TransactionAggregationServiceTests
{
    private static ITransactionAggregationService BuildService()
    {
        var env = EnvironmentMocks.CreateMockEnv(Directory.GetCurrentDirectory());
        var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(new NullLoggerProvider()));

        ITransactionSourceAdapter[] sources =
        {
            new ASourceAdapter(env, loggerFactory.CreateLogger<ASourceAdapter>()),
            new BSourceAdapter(env, loggerFactory.CreateLogger<BSourceAdapter>()),
            new CSourceAdapter(env, loggerFactory.CreateLogger<CSourceAdapter>())
        };

        return new TransactionAggregationService(sources);
    }

    [Fact]
    public async Task AggregationService_Should_Merge_All_Three_Sources()
    {
        var svc = BuildService();

        var result = await svc.GetAllAsync("C1");

        Assert.NotEmpty(result);

        // All three sources must contribute
        Assert.Contains(result, t => t.SourceName == "ASource");
        Assert.Contains(result, t => t.SourceName == "BSource");
        Assert.Contains(result, t => t.SourceName == "CSource");

        // Currency always ZAR
        Assert.All(result, r => Assert.Equal("ZAR", r.Currency));
    }

    [Fact]
    public async Task AggregationService_Should_Return_Newest_First()
    {
        var svc = BuildService();

        var result = await svc.GetAllAsync("C1");
        var sorted = result.OrderByDescending(t => t.Timestamp).ToList();

        Assert.Equal(sorted, result);
    }

    [Fact]
    public async Task AggregationService_Should_Filter_By_Date_Range()
    {
        var svc = BuildService();

        var from = new DateTime(2025, 11, 02);
        var to   = new DateTime(2025, 12, 01);

        var result = await svc.GetAllAsync("C1", from, to);

        Assert.NotEmpty(result);
        Assert.All(result, txn =>
        {
            Assert.True(txn.Timestamp >= from);
            Assert.True(txn.Timestamp <= to);
        });
    }

    private sealed class NullLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => NullLogger.Instance;
        public void Dispose()
        {
        }
    }
}