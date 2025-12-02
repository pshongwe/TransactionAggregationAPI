using System.Net;
using System.Net.Http.Json;
using TransactionAggregation.Api.Dtos;
using TransactionAggregation.Tests.TestServer;
using Xunit;

namespace TransactionAggregation.Tests.Integration;

public class GetSummaryTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public GetSummaryTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Summary_Returns_Grouped_Categories()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/customers/C1/transactions/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var summary = await response.Content.ReadFromJsonAsync<List<CategorySummaryDto>>();

        Assert.NotNull(summary);
        Assert.NotEmpty(summary);

        // Validate structure
        Assert.All(summary, s => Assert.False(string.IsNullOrWhiteSpace(s.Category)));
        Assert.All(summary, s => Assert.True(s.TransactionCount >= 1));
    }
}