using TransactionAggregation.Tests.TestUtils;
using TransactionAggregation.Api.Mapping;
using Xunit;

namespace TransactionAggregation.Tests.Mapping;

public class DtoMapperTests
{
    [Fact]
    public void Mapping_Should_Produce_Iso_Dates()
    {
        // Arrange
        var timestamp = new DateTime(2025, 01, 15, 13, 45, 20, DateTimeKind.Utc);

        var txn = UnifiedTransactionBuilder.Default()
            .WithTransactionId("t123")
            .WithCustomerId("cust1")
            .WithAmount(250.75m)
            .WithCurrency("ZAR")
            .WithDescription("Test transaction")
            .WithCategory("Food")
            .WithSource("ASource")
            .WithTimestamp(timestamp)
            .Build();

        // Act
        var dto = txn.ToDto();

        // Assert
        Assert.Equal("2025-01-15T13:45:20Z", dto.Date);
        Assert.Equal(250.75m, dto.Amount);
        Assert.Equal("ZAR", dto.Currency);
        Assert.Equal("Test transaction", dto.Description);
        Assert.Equal("Food", dto.Category);
        Assert.Equal("ASource", dto.Source);
    }
}