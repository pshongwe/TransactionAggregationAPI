using System.Net;
using System.Net.Http.Json;
using TransactionAggregation.Api.Dtos;
using TransactionAggregation.Tests.TestServer;
using Xunit;

namespace TransactionAggregation.Tests.Integration;

public class GetTransactionsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public GetTransactionsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetTransactions_Returns_Transactions_For_Valid_Customer()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/customers/C1/transactions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var txns = await response.Content.ReadFromJsonAsync<List<TransactionDto>>();

        Assert.NotNull(txns);
        Assert.NotEmpty(txns);

        // Validate DTO correctness
        Assert.All(txns, t => Assert.False(string.IsNullOrWhiteSpace(t.Id)));
        Assert.All(txns, t => Assert.Equal("ZAR", t.Currency));
        Assert.All(txns, t => Assert.False(string.IsNullOrWhiteSpace(t.Category)));
        Assert.All(txns, t => Assert.False(string.IsNullOrWhiteSpace(t.Source)));
    }

    [Fact]
    public async Task GetTransactions_Applies_Date_Filter()
    {
        var client = _factory.CreateClient();

        var url = "/customers/C1/transactions?from=2025-11-02&to=2025-12-01";
        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var txns = await response.Content.ReadFromJsonAsync<List<TransactionDto>>();

        Assert.NotNull(txns);
        Assert.All(txns, t =>
        {
            var ts = DateTime.Parse(t.Date);
            Assert.True(ts >= new DateTime(2025, 11, 2));
            Assert.True(ts <= new DateTime(2025, 12, 1));
        });
    }
}