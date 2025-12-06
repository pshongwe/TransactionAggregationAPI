using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
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
        var logger = new Mock<ILogger<BSourceAdapter>>();

        var adapter = new BSourceAdapter(env.Object, logger.Object);

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

    [Fact]
    public async Task Adapter_Returns_Empty_When_File_Contains_Null_Root()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempRoot);

        var mockDir = Path.Combine(tempRoot, "MockData");
        Directory.CreateDirectory(mockDir);

        var filePath = Path.Combine(mockDir, "BSource.json");
        await File.WriteAllTextAsync(filePath, "null");

        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.ContentRootPath).Returns(tempRoot);
        var logger = new Mock<ILogger<BSourceAdapter>>();

        var adapter = new BSourceAdapter(env.Object, logger.Object);

        var result = await adapter.FetchAndNormalizeAsync("cust789");

        Assert.Empty(result);
    }

    [Fact]
    public async Task Adapter_Returns_Empty_When_Customer_Not_Found()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempRoot);

        var mockDir = Path.Combine(tempRoot, "MockData");
        Directory.CreateDirectory(mockDir);

        var filePath = Path.Combine(mockDir, "BSource.json");

        var jsonData = """
        [
            {
                "id": "B200",
                "customer": "cust999",
                "value": 455.75,
                "timestamp": "2024-07-10T08:45:00Z",
                "merchant": "Shoprite"
            }
        ]
        """;

        await File.WriteAllTextAsync(filePath, jsonData);

        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.ContentRootPath).Returns(tempRoot);
        var logger = new Mock<ILogger<BSourceAdapter>>();

        var adapter = new BSourceAdapter(env.Object, logger.Object);

        var result = await adapter.FetchAndNormalizeAsync("cust789");

        Assert.Empty(result);
    }

    [Fact]
    public async Task Adapter_Skips_Null_Array_Items()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempRoot);

        var mockDir = Path.Combine(tempRoot, "MockData");
        Directory.CreateDirectory(mockDir);

        var filePath = Path.Combine(mockDir, "BSource.json");

        var jsonData = """
        [
            null,
            {
                "id": "B301",
                "customer": "cust789",
                "value": 10,
                "timestamp": "2024-02-01T00:00:00Z",
                "merchant": "Checkers"
            }
        ]
        """;

        await File.WriteAllTextAsync(filePath, jsonData);

        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.ContentRootPath).Returns(tempRoot);
        var logger = new Mock<ILogger<BSourceAdapter>>();

        var adapter = new BSourceAdapter(env.Object, logger.Object);

        var result = await adapter.FetchAndNormalizeAsync("cust789");

        Assert.Single(result);
        Assert.Equal("B301", result[0].TransactionId);
    }

    [Fact]
    public async Task Adapter_Returns_Empty_When_Customer_Field_Missing()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempRoot);
        var mockDir = Path.Combine(tempRoot, "MockData");
        Directory.CreateDirectory(mockDir);
        var filePath = Path.Combine(mockDir, "BSource.json");

        var jsonData = """
        [
            {
                "id": "B400",
                "value": 10,
                "timestamp": "2024-01-01T00:00:00Z",
                "merchant": "No Customer"
            }
        ]
        """;

        await File.WriteAllTextAsync(filePath, jsonData);

        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.ContentRootPath).Returns(tempRoot);
        var logger = new Mock<ILogger<BSourceAdapter>>();

        var adapter = new BSourceAdapter(env.Object, logger.Object);

        var result = await adapter.FetchAndNormalizeAsync("cust789");

        Assert.Empty(result);
    }

    [Fact]
    public async Task Adapter_Assigns_Defaults_When_Id_And_Value_Missing()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempRoot);
        var mockDir = Path.Combine(tempRoot, "MockData");
        Directory.CreateDirectory(mockDir);
        var filePath = Path.Combine(mockDir, "BSource.json");

        var jsonData = """
        [
            {
                "customer": "cust789",
                "timestamp": "2024-03-01T10:00:00Z",
                "merchant": "Pick n Pay"
            }
        ]
        """;

        await File.WriteAllTextAsync(filePath, jsonData);

        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.ContentRootPath).Returns(tempRoot);
        var logger = new Mock<ILogger<BSourceAdapter>>();

        var adapter = new BSourceAdapter(env.Object, logger.Object);

        var result = await adapter.FetchAndNormalizeAsync("cust789");

        Assert.Single(result);
        Assert.True(Guid.TryParse(result[0].TransactionId, out _));
        Assert.Equal(0m, result[0].Amount);
    }
}