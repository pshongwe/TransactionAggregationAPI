using Microsoft.AspNetCore.Hosting;
using Moq;
using TransactionAggregation.Api.Adapters;
using TransactionAggregation.Domain.Models;
using Xunit;

namespace TransactionAggregation.Tests.Adapters;

public class BSourceAdapterTests
{
    [Fact]
    public async Task Adapter_Should_Map_BSource_File_To_UnifiedTransaction()
    {
        // Arrange
        var customerId = "cust789";

        // Temp directory
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempRoot);

        var mockDir = Path.Combine(tempRoot, "MockData");
        Directory.CreateDirectory(mockDir);

        var filePath = Path.Combine(mockDir, "BSource.json");

        var jsonData = """
        [
            {
                "id": "B200",
                "customer": "cust789",
                "value": 455.75,
                "timestamp": "2024-07-10T08:45:00Z",
                "merchant": "Shoprite"
            },
            {
                "id": "IGNORE",
                "customer": "another",
                "value": 123,
                "timestamp": "2024-01-01T00:00:00Z",
                "merchant": "Ignore Merchant"
            }
        ]
        """;

        await File.WriteAllTextAsync(filePath, jsonData);

        // Mock IWebHostEnvironment
        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.ContentRootPath).Returns(tempRoot);

        var adapter = new BSourceAdapter(env.Object);

        // Act
        var result = await adapter.FetchAndNormalizeAsync(customerId);

        // Assert
        Assert.Single(result);

        var txn = result.First();

        Assert.Equal("B200", txn.TransactionId);
        Assert.Equal("cust789", txn.CustomerId);
        Assert.Equal(455.75m, txn.Amount);
        Assert.Equal("ZAR", txn.Currency);
        Assert.Equal(DateTime.Parse("2024-07-10T08:45:00Z"), txn.Timestamp);
        Assert.Equal("Shoprite", txn.Description);
        Assert.Equal("BSource", txn.SourceName);

        Assert.False(string.IsNullOrWhiteSpace(txn.Category));
    }
}