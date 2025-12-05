using Microsoft.AspNetCore.Hosting;
using Moq;
using TransactionAggregation.Api.Adapters;
using TransactionAggregation.Domain.Models;
using Xunit;

namespace TransactionAggregation.Tests.Adapters;

public class CSourceAdapterTests
{
    [Fact]
    public async Task Adapter_Should_Map_CSource_File_To_UnifiedTransaction()
    {
        // Arrange
        var customerId = "acct555";

        // Setup temp directory
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempRoot);

        var mockDir = Path.Combine(tempRoot, "MockData");
        Directory.CreateDirectory(mockDir);

        var filePath = Path.Combine(mockDir, "CSource.json");

        // CSource format is different: root object, entries array
        var jsonData = """
        {
            "account": "acct555",
            "entries": [
                {
                    "amt": 300.50,
                    "date": "2023-11-10T15:00:00Z",
                    "desc": "Uber Ride"
                },
                {
                    "amt": 750.00,
                    "date": "2023-11-11T10:00:00Z",
                    "desc": "Flight Ticket"
                }
            ]
        }
        """;

        await File.WriteAllTextAsync(filePath, jsonData);

        // Mock environment
        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.ContentRootPath).Returns(tempRoot);

        var adapter = new CSourceAdapter(env.Object);

        // Act
        var result = await adapter.FetchAndNormalizeAsync(customerId);

        // Assert
        Assert.Equal(2, result.Count);

        var first = result[0];
        Assert.Equal("acct555", first.CustomerId);
        Assert.Equal(300.50m, first.Amount);
        Assert.Equal("Uber Ride", first.Description);
        Assert.Equal("CSource", first.SourceName);

        var second = result[1];
        Assert.Equal("acct555", second.CustomerId);
        Assert.Equal(750m, second.Amount);
        Assert.Equal("Flight Ticket", second.Description);
        Assert.Equal("CSource", second.SourceName);

        Assert.False(string.IsNullOrWhiteSpace(first.Category));
        Assert.False(string.IsNullOrWhiteSpace(second.Category));
    }

    [Fact]
    public async Task Adapter_Returns_Empty_When_File_Contains_Null_Root()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempRoot);

        var mockDir = Path.Combine(tempRoot, "MockData");
        Directory.CreateDirectory(mockDir);

        var filePath = Path.Combine(mockDir, "CSource.json");
        await File.WriteAllTextAsync(filePath, "null");

        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.ContentRootPath).Returns(tempRoot);

        var adapter = new CSourceAdapter(env.Object);

        var result = await adapter.FetchAndNormalizeAsync("acct555");

        Assert.Empty(result);
    }

    [Fact]
    public async Task Adapter_Returns_Empty_When_Root_Missing_Entries()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempRoot);

        var mockDir = Path.Combine(tempRoot, "MockData");
        Directory.CreateDirectory(mockDir);

        var filePath = Path.Combine(mockDir, "CSource.json");

        var jsonData = """
        {
            "account": "acct555"
        }
        """;

        await File.WriteAllTextAsync(filePath, jsonData);

        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.ContentRootPath).Returns(tempRoot);

        var adapter = new CSourceAdapter(env.Object);

        var result = await adapter.FetchAndNormalizeAsync("acct555");

        Assert.Empty(result);
    }

    [Fact]
    public async Task Adapter_Skips_Null_Entry_Items()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempRoot);

        var mockDir = Path.Combine(tempRoot, "MockData");
        Directory.CreateDirectory(mockDir);

        var filePath = Path.Combine(mockDir, "CSource.json");

        var jsonData = """
        {
            "account": "acct555",
            "entries": [
                null,
                {
                    "amt": 100,
                    "date": "2023-01-01T00:00:00Z",
                    "desc": "Uber"
                }
            ]
        }
        """;

        await File.WriteAllTextAsync(filePath, jsonData);

        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.ContentRootPath).Returns(tempRoot);

        var adapter = new CSourceAdapter(env.Object);

        var result = await adapter.FetchAndNormalizeAsync("acct555");

        Assert.Single(result);
        Assert.Equal("Uber", result[0].Description);
    }

    [Fact]
    public async Task Adapter_Returns_Empty_When_Account_Blank()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempRoot);
        var mockDir = Path.Combine(tempRoot, "MockData");
        Directory.CreateDirectory(mockDir);
        var filePath = Path.Combine(mockDir, "CSource.json");

        var jsonData = """
        {
            "account": "",
            "entries": []
        }
        """;

        await File.WriteAllTextAsync(filePath, jsonData);

        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.ContentRootPath).Returns(tempRoot);

        var adapter = new CSourceAdapter(env.Object);

        var result = await adapter.FetchAndNormalizeAsync("acct555");

        Assert.Empty(result);
    }

    [Fact]
    public async Task Adapter_Throws_When_Date_Invalid()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempRoot);
        var mockDir = Path.Combine(tempRoot, "MockData");
        Directory.CreateDirectory(mockDir);
        var filePath = Path.Combine(mockDir, "CSource.json");

        var jsonData = """
        {
            "account": "acct555",
            "entries": [
                {
                    "amt": 10,
                    "date": "bad-date",
                    "desc": "Invalid"
                }
            ]
        }
        """;

        await File.WriteAllTextAsync(filePath, jsonData);

        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.ContentRootPath).Returns(tempRoot);

        var adapter = new CSourceAdapter(env.Object);

        await Assert.ThrowsAsync<FormatException>(() => adapter.FetchAndNormalizeAsync("acct555"));
    }
}