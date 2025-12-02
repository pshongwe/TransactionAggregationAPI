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
}