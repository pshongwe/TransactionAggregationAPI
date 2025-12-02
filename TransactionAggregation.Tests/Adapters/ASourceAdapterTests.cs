using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Moq;
using TransactionAggregation.Api.Adapters;
using TransactionAggregation.Domain.Models;
using Xunit;

namespace TransactionAggregation.Tests.Adapters;

public class ASourceAdapterTests
{
    [Fact]
    public async Task Adapter_Should_Map_ASource_File_To_UnifiedTransaction()
    {
        // Arrange
        var customerId = "cust123";

        // Build temp directory
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempRoot);

        // Build MockData folder
        var mockDir = Path.Combine(tempRoot, "MockData");
        Directory.CreateDirectory(mockDir);

        // Create mock data file
        var filePath = Path.Combine(mockDir, "ASource.json");

        var jsonData = """
        [
            {
                "txn_id": "A001",
                "cust": "cust123",
                "amount": 199.99,
                "when": "2025-10-01T12:30:00Z",
                "text": "ASource purchase"
            },
            {
                "txn_id": "OTHER",
                "cust": "someone_else",
                "amount": 999,
                "when": "2025-01-01T00:00:00Z",
                "text": "Ignore this"
            }
        ]
        """;

        await File.WriteAllTextAsync(filePath, jsonData);

        // Mock environment
        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.ContentRootPath).Returns(tempRoot);

        // Adapter under test
        var adapter = new ASourceAdapter(env.Object);

        // Act
        var result = await adapter.FetchAndNormalizeAsync(customerId);

        // Assert
        Assert.Single(result);

        var txn = result.First();

        Assert.Equal("A001", txn.TransactionId);
        Assert.Equal("cust123", txn.CustomerId);
        Assert.Equal(199.99m, txn.Amount);
        Assert.Equal("ZAR", txn.Currency);
        Assert.Equal(DateTime.Parse("2025-10-01T12:30:00Z"), txn.Timestamp);
        Assert.Equal("ASource purchase", txn.Description);
        Assert.Equal("ASource", txn.SourceName);

        // Ensure categorizer was applied
        Assert.False(string.IsNullOrWhiteSpace(txn.Category));
    }
}