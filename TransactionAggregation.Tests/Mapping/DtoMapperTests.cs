using TransactionAggregation.Tests.TestUtils;
using TransactionAggregation.Api.Mapping;
using TransactionAggregation.Domain.Models;
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

    [Fact]
    public void Mapping_Should_Convert_NonUtc_Timestamps_To_Utc()
    {
        // Arrange
        // Create a UTC time: 2025-01-15 11:45:20 UTC
        // This represents 13:45:20 in UTC+2 timezone
        var utcTime = new DateTime(2025, 01, 15, 11, 45, 20, DateTimeKind.Utc);

        var txn = UnifiedTransactionBuilder.Default()
            .WithTransactionId("t456")
            .WithAmount(100m)
            .WithCurrency("USD")
            .WithTimestamp(utcTime)
            .Build();

        // Act
        var dto = txn.ToDto();

        // Assert - Should always be in UTC format with Z suffix
        Assert.EndsWith("Z", dto.Date);
        Assert.Equal("2025-01-15T11:45:20Z", dto.Date);
    }

    [Theory]
    [InlineData(2025, 1, 15, 0, 0, 0, "2025-01-15T00:00:00Z")]
    [InlineData(2025, 1, 15, 23, 59, 59, "2025-01-15T23:59:59Z")]
    [InlineData(2024, 12, 31, 23, 59, 59, "2024-12-31T23:59:59Z")]
    [InlineData(2025, 2, 1, 12, 30, 45, "2025-02-01T12:30:45Z")]
    public void Mapping_Should_Format_Various_Timestamps_Correctly(int year, int month, int day, int hour, int minute, int second, string expectedDate)
    {
        // Arrange
        var timestamp = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
        var txn = UnifiedTransactionBuilder.Default()
            .WithTransactionId("t-date-test")
            .WithAmount(50m)
            .WithCurrency("USD")
            .WithTimestamp(timestamp)
            .Build();

        // Act
        var dto = txn.ToDto();

        // Assert
        Assert.Equal(expectedDate, dto.Date);
    }

    [Theory]
    [InlineData("USD", 100.50, "Grocery Store")]
    [InlineData("EUR", 50.00, "Gas Station")]
    [InlineData("GBP", 1000.99, "Restaurant")]
    [InlineData("JPY", 10000.0, "Hotel")]
    [InlineData("ZAR", 0.01, "Minimum amount")]
    public void Mapping_Should_Handle_Various_Currencies_And_Amounts(string currency, double amountDouble, string description)
    {
        // Arrange
        var amount = (decimal)amountDouble;
        var timestamp = new DateTime(2025, 01, 15, 10, 00, 00, DateTimeKind.Utc);
        var txn = UnifiedTransactionBuilder.Default()
            .WithTransactionId("t789")
            .WithAmount(amount)
            .WithCurrency(currency)
            .WithDescription(description)
            .WithTimestamp(timestamp)
            .Build();

        // Act
        var dto = txn.ToDto();

        // Assert
        Assert.Equal(currency, dto.Currency);
        Assert.Equal(amount, dto.Amount);
        Assert.Equal(description, dto.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("A very long description with special characters: !@#$%^&*()")]
    public void Mapping_Should_Handle_Various_Descriptions(string description)
    {
        // Arrange
        var timestamp = new DateTime(2025, 01, 15, 10, 00, 00, DateTimeKind.Utc);
        var txn = UnifiedTransactionBuilder.Default()
            .WithTransactionId("t101")
            .WithAmount(50m)
            .WithCurrency("USD")
            .WithDescription(description)
            .WithTimestamp(timestamp)
            .Build();

        // Act
        var dto = txn.ToDto();

        // Assert
        Assert.Equal(description, dto.Description);
    }

    [Fact]
    public void Mapping_Should_Preserve_All_Fields_Correctly()
    {
        // Arrange
        var timestamp = new DateTime(2025, 01, 15, 13, 45, 20, DateTimeKind.Utc);
        var txn = UnifiedTransactionBuilder.Default()
            .WithTransactionId("txn-unique-id")
            .WithCustomerId("customer-123")
            .WithAmount(999.99m)
            .WithCurrency("ZAR")
            .WithDescription("Online Purchase")
            .WithCategory("Shopping")
            .WithSource("BSource")
            .WithTimestamp(timestamp)
            .Build();

        // Act
        var dto = txn.ToDto();

        // Assert - Verify all fields are preserved
        Assert.Equal("txn-unique-id", dto.Id);
        Assert.Equal(999.99m, dto.Amount);
        Assert.Equal("2025-01-15T13:45:20Z", dto.Date);
        Assert.Equal("ZAR", dto.Currency);
        Assert.Equal("Online Purchase", dto.Description);
        Assert.Equal("Shopping", dto.Category);
        Assert.Equal("BSource", dto.Source);
    }

    [Fact]
    public void Mapping_Collections_Should_Preserve_Multiple_Transactions()
    {
        // Arrange
        var timestamp = new DateTime(2025, 01, 15, 10, 00, 00, DateTimeKind.Utc);
        var txns = new[]
        {
            UnifiedTransactionBuilder.Default()
                .WithTransactionId("t1")
                .WithAmount(100m)
                .WithCurrency("USD")
                .WithTimestamp(timestamp)
                .Build(),
            UnifiedTransactionBuilder.Default()
                .WithTransactionId("t2")
                .WithAmount(200m)
                .WithCurrency("EUR")
                .WithTimestamp(timestamp.AddHours(1))
                .Build(),
            UnifiedTransactionBuilder.Default()
                .WithTransactionId("t3")
                .WithAmount(300m)
                .WithCurrency("GBP")
                .WithTimestamp(timestamp.AddHours(2))
                .Build()
        };

        // Act
        var dtos = txns.ToDto().ToList();

        // Assert
        Assert.Equal(3, dtos.Count);
        Assert.Equal("t1", dtos[0].Id);
        Assert.Equal(100m, dtos[0].Amount);
        Assert.Equal("t2", dtos[1].Id);
        Assert.Equal(200m, dtos[1].Amount);
        Assert.Equal("t3", dtos[2].Id);
        Assert.Equal(300m, dtos[2].Amount);
    }

    [Fact]
    public void Mapping_Collections_Should_Handle_Empty_Enumerable()
    {
        // Arrange
        var emptyTxns = Array.Empty<UnifiedTransaction>();

        // Act
        var dtos = emptyTxns.ToDto().ToList();

        // Assert
        Assert.Empty(dtos);
    }
}