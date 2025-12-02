using Moq;
using TransactionAggregation.Domain.Abstractions;
using TransactionAggregation.Domain.Services;
using TransactionAggregation.Tests.TestUtils;
using Xunit;

namespace TransactionAggregation.Tests.Aggregation;

public class AggregationServiceTests
{
    [Fact]
    public async Task AggregationService_Merges_All_Sources()
    {
        // Arrange
        var customerId = "cust123";

        var txn1 = UnifiedTransactionBuilder.Default()
            .WithCustomerId(customerId)
            .WithTransactionId("t1")
            .WithAmount(10)
            .WithSource("ASource")
            .Build();

        var txn2 = UnifiedTransactionBuilder.Default()
            .WithCustomerId(customerId)
            .WithTransactionId("t2")
            .WithAmount(20)
            .WithSource("BSource")
            .Build();

        var txn3 = UnifiedTransactionBuilder.Default()
            .WithCustomerId(customerId)
            .WithTransactionId("t3")
            .WithAmount(30)
            .WithSource("CSource")
            .Build();

        var sourceA = new Mock<ITransactionSourceAdapter>();
        var sourceB = new Mock<ITransactionSourceAdapter>();
        var sourceC = new Mock<ITransactionSourceAdapter>();

        sourceA.Setup(s => s.FetchAndNormalizeAsync(customerId, default))
               .ReturnsAsync(new[] { txn1 });

        sourceB.Setup(s => s.FetchAndNormalizeAsync(customerId, default))
               .ReturnsAsync(new[] { txn2 });

        sourceC.Setup(s => s.FetchAndNormalizeAsync(customerId, default))
               .ReturnsAsync(new[] { txn3 });

        var sut = new TransactionAggregationService(new[]
        {
            sourceA.Object,
            sourceB.Object,
            sourceC.Object
        });

        // Act
        var result = await sut.GetAllAsync(customerId);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, x => x.TransactionId == "t1" && x.SourceName == "ASource");
        Assert.Contains(result, x => x.TransactionId == "t2" && x.SourceName == "BSource");
        Assert.Contains(result, x => x.TransactionId == "t3" && x.SourceName == "CSource");
    }

    [Fact]
    public async Task AggregationService_Applies_Date_Filter()
    {
        // Arrange
        var customerId = "cust123";

        var early = UnifiedTransactionBuilder.Default()
            .WithCustomerId(customerId)
            .WithTransactionId("early")
            .WithTimestamp(new DateTime(2020, 1, 1))
            .Build();

        var inside = UnifiedTransactionBuilder.Default()
            .WithCustomerId(customerId)
            .WithTransactionId("inside")
            .WithTimestamp(new DateTime(2020, 6, 1))
            .Build();

        var late = UnifiedTransactionBuilder.Default()
            .WithCustomerId(customerId)
            .WithTransactionId("late")
            .WithTimestamp(new DateTime(2021, 1, 1))
            .Build();

        var source = new Mock<ITransactionSourceAdapter>();
        source.Setup(s => s.FetchAndNormalizeAsync(customerId, default))
              .ReturnsAsync(new[] { early, inside, late });

        var sut = new TransactionAggregationService(new[] { source.Object });

        var from = new DateTime(2020, 2, 1);
        var to = new DateTime(2020, 12, 31);

        // Act
        var result = await sut.GetAllAsync(customerId, from, to);

        // Assert
        Assert.Single(result);
        Assert.Equal("inside", result.First().TransactionId);
    }
}